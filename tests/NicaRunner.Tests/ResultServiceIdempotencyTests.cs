using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class ResultServiceIdempotencyTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _dashboardNotifier = new();

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object, _dashboardNotifier.Object);

    private void RaceExists(int raceId = 1, RaceStatus estado = RaceStatus.EnCurso)
    {
        _races.Setup(r => r.GetByIdAsync(raceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race
            {
                Id = raceId,
                Nombre = "Test",
                AdminId = 1,
                Estado = estado,
                RaceStartUtc = new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc)
            });
    }

    private static CreateResultRequest MakeRequest(string? dorsal = null) => new(dorsal);

    // POST sin Idempotency-Key: comportamiento legacy preservado. IdempotencyKey
    // queda NULL en el Result persistido y nunca se consulta GetByIdempotencyKeyAsync.
    [Fact]
    public async Task Create_SinKey_CreaResultConIdempotencyKeyNull()
    {
        RaceExists();
        Result? added = null;
        _results.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()))
            .Callback<Result, CancellationToken>((r, _) => { added = r; r.Id = 42; })
            .Returns(Task.CompletedTask);
        _results.Setup(r => r.GetByIdAsync(1, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added);

        var dto = await BuildService().CreateAsync(1, MakeRequest(), capturistaId: 5);

        Assert.NotNull(added);
        Assert.Null(added!.IdempotencyKey);
        Assert.Equal(42, dto.Id);
        _results.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _results.Verify(r => r.SaveNewResultAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // POST con Idempotency-Key nuevo: hace lookup (falla), crea normal y
    // persiste el key en la fila.
    [Fact]
    public async Task Create_ConKeyNuevo_PersisteKeyEnResult()
    {
        RaceExists();
        _results.Setup(r => r.GetByIdempotencyKeyAsync(1, "key-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result?)null);

        Result? added = null;
        _results.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()))
            .Callback<Result, CancellationToken>((r, _) => { added = r; r.Id = 100; })
            .Returns(Task.CompletedTask);
        _results.Setup(r => r.GetByIdAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added);

        var dto = await BuildService().CreateAsync(1, MakeRequest(), capturistaId: 5, idempotencyKey: "key-abc");

        Assert.Equal("key-abc", added!.IdempotencyKey);
        Assert.Equal(100, dto.Id);
    }

    // POST reintentado con el mismo Idempotency-Key ya usado: devuelve el
    // Result existente sin re-validar dorsal ni ejecutar SaveNewResultAsync.
    // Esto es el corazón de la feature: reintentos post-timeout son no-op.
    [Fact]
    public async Task Create_ConKeyExistente_DevuelveResultSinReValidar()
    {
        RaceExists();
        var existing = new Result
        {
            Id = 77,
            RaceId = 1,
            RunnerId = 3,
            Dorsal = "42",
            TiempoLlegada = new DateTime(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc),
            CategoryId = 2,
            CapturistaId = 5,
            IdempotencyKey = "key-abc"
        };
        _results.Setup(r => r.GetByIdempotencyKeyAsync(1, "key-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // El cliente puede haber cambiado el body del retry (ej. corrigió el
        // dorsal) — igual devolvemos el original. Contrato: "misma key = misma
        // captura". Si mando un dorsal nuevo con la misma key, no se re-lee ni
        // se busca el corredor: prueba de que el service NUNCA llamó a runners
        // ni intentó guardar nada.
        var dto = await BuildService().CreateAsync(1, MakeRequest(dorsal: "999"), capturistaId: 5, idempotencyKey: "key-abc");

        Assert.Equal(77, dto.Id);
        Assert.Equal("42", dto.Dorsal);
        _runners.Verify(r => r.GetByDorsalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _results.Verify(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()), Times.Never);
        _results.Verify(r => r.SaveNewResultAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Race condition: SaveNewResultAsync lanza IdempotencyConflictException
    // (dos POSTs concurrentes con el mismo key, uno perdió el UK). El service
    // re-lee al ganador y devuelve ese DTO. RecalculatePositions no corre —
    // el ganador ya lo hizo.
    [Fact]
    public async Task Create_KeyConcurrentRace_RelesYDevuelveGanador()
    {
        RaceExists();
        _results.Setup(r => r.GetByIdempotencyKeyAsync(1, "key-race", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result?)null);
        _results.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _results.Setup(r => r.SaveNewResultAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdempotencyConflictException());

        var winner = new Result
        {
            Id = 55,
            RaceId = 1,
            TiempoLlegada = new DateTime(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc),
            CapturistaId = 9,
            IdempotencyKey = "key-race"
        };
        // Primer lookup: null (nadie usó el key todavía cuando entramos).
        // Segundo lookup (después del conflict): el ganador ya está commiteado.
        _results.SetupSequence(r => r.GetByIdempotencyKeyAsync(1, "key-race", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result?)null)
            .ReturnsAsync(winner);

        var dto = await BuildService().CreateAsync(1, MakeRequest(), capturistaId: 5, idempotencyKey: "key-race");

        Assert.Equal(55, dto.Id);
        Assert.Equal(9, dto.CapturistaId);
    }

    // Estado imposible: SaveNewResultAsync lanza IdempotencyConflictException
    // pero al re-leer no aparece nadie con ese key. Merece visibilidad — se
    // burbujea la excepción original en vez de enmascararla como éxito.
    [Fact]
    public async Task Create_KeyConcurrentRaceSinGanador_ReLanzaExcepcion()
    {
        RaceExists();
        _results.Setup(r => r.GetByIdempotencyKeyAsync(1, "key-fantasma", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result?)null);
        _results.Setup(r => r.SaveNewResultAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdempotencyConflictException());

        await Assert.ThrowsAsync<IdempotencyConflictException>(
            () => BuildService().CreateAsync(1, MakeRequest(), capturistaId: 5, idempotencyKey: "key-fantasma"));
    }

    // Condición de carrera sin Idempotency-Key: dos capturas casi simultáneas
    // del mismo dorsal pasan el ExistsByRunnerAsync antes de que la otra
    // comitee. La constraint de BD (RaceId, RunnerId) la atrapa en
    // SaveNewResultAsync — a diferencia del conflicto de idempotencia, acá no
    // hay ganador que re-leer: el perdedor debe recibir el mismo 409 que ya
    // usa el chequeo previo.
    [Fact]
    public async Task Create_DorsalConcurrente_LanzaConflictException()
    {
        RaceExists();
        var runner = new Runner { Id = 3, RaceId = 1, Dorsal = "101", CategoryId = 2, Nombre = "Juan" };
        _runners.Setup(r => r.GetByDorsalAsync(1, "101", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.ExistsByRunnerAsync(1, 3, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _results.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _results.Setup(r => r.SaveNewResultAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RunnerResultConflictException());

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => BuildService().CreateAsync(1, MakeRequest(dorsal: "101"), capturistaId: 5));

        Assert.Contains("101", ex.Message);
    }

    // Sanity: sin key, una DbUpdateException (bug real, no idempotency)
    // NO se atrapa. La constraint `when (!string.IsNullOrWhiteSpace(idempotencyKey))`
    // deja pasar cualquier problema cuando no hay key.
    [Fact]
    public async Task Create_SinKey_ExcepcionDeRepoNoSeEnmascara()
    {
        RaceExists();
        _results.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _results.Setup(r => r.SaveNewResultAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdempotencyConflictException());

        // Sin idempotencyKey el catch no aplica → la excepción burbujea tal cual.
        await Assert.ThrowsAsync<IdempotencyConflictException>(
            () => BuildService().CreateAsync(1, MakeRequest(), capturistaId: 5));
    }

    // El servidor es la única fuente del instante de llegada: el request no
    // lleva ningún tiempo, así que no hay valor de cliente que validar ni
    // rechazar por estar en el pasado o en el futuro. TiempoLlegada sale del
    // reloj del servidor en el momento del POST.
    [Fact]
    public async Task Create_FijaTiempoLlegadaConElRelojDelServidor()
    {
        RaceExists(1);
        var before = DateTime.UtcNow;
        Result? added = null;
        _results.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()))
            .Callback<Result, CancellationToken>((r, _) => { added = r; r.Id = 1; })
            .Returns(Task.CompletedTask);
        _results.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added);

        await BuildService().CreateAsync(1, MakeRequest(), capturistaId: 5);
        var after = DateTime.UtcNow;

        Assert.NotNull(added);
        Assert.InRange(added!.TiempoLlegada, before, after);
    }

    // Sin RaceStartUtc no hay contra qué medir una llegada: la carrera nunca arrancó.
    [Fact]
    public async Task Create_CarreraSinArrancar_LanzaValidationException()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "Test", AdminId = 1 });

        await Assert.ThrowsAsync<ValidationException>(
            () => BuildService().CreateAsync(1, MakeRequest(), capturistaId: 5));

        _results.Verify(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Terminada may still have RaceStartUtc; capture must refuse when Estado != EnCurso.
    [Fact]
    public async Task Create_CarreraTerminada_LanzaValidationException()
    {
        RaceExists(estado: RaceStatus.Terminada);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => BuildService().CreateAsync(1, MakeRequest(), capturistaId: 5));

        Assert.Contains("EnCurso", ex.Message);
        _results.Verify(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
