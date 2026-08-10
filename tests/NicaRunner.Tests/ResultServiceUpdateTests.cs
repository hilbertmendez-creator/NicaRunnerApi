using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class ResultServiceUpdateTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _dashboardNotifier = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<IAdminNotificationService> _adminNotifications = new();

    public ResultServiceUpdateTests()
    {
        // Default sin arrancar ninguna categoría: estos tests no ejercitan ElapsedMillis,
        // solo necesitan que ToDto no reviente por un lookup null (Task 5).
        _raceCategories.Setup(rc => rc.GetAssociationsByRaceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RaceCategory>());
    }

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object, _dashboardNotifier.Object, _raceCategories.Object, _adminNotifications.Object);

    // La mayoría de los tests de este archivo ejercitan el camino feliz de asignación
    // de dorsal (no las disputas de F2/F3, que tienen su propio archivo,
    // ResultDisputeTests.cs) — todos necesitan que la categoría esté EnCurso para que
    // TryResolveDorsalAsync no los desvíe a Controversia por "categoría sin salida".
    private void CategoryEnCurso(int categoryId) =>
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RaceCategory
            {
                RaceId = 1, CategoryId = categoryId, Estado = RaceCategoryStatus.EnCurso,
                Category = new Category { Id = categoryId, NombreCategoria = "Test" }
            });

    private void RaceExists(int raceId = 1) =>
        _races.Setup(r => r.GetByIdAsync(raceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = raceId, Nombre = "Test", AdminId = 1 });

    // Mismo dorsal (mismo corredor/categoría), solo cambia el tiempo: se
    // audita únicamente el campo que cambió y se recalcula una sola vez.
    [Fact]
    public async Task Update_SoloCambiaTiempo_AuditaSoloEseCampoYRecalculaUnaVez()
    {
        RaceExists();
        var existing = new Result
        {
            Id = 10, RaceId = 1, RunnerId = 5, Dorsal = "101",
            TiempoLlegada = DateTime.UtcNow.AddHours(-2), CategoryId = 2, CapturistaId = 9
        };
        _results.Setup(r => r.GetByIdAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var runner = new Runner { Id = 5, RaceId = 1, Dorsal = "101", CategoryId = 2, Nombre = "Juan" };
        _runners.Setup(r => r.GetByDorsalAsync(1, "101", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        CategoryEnCurso(2);

        _results.Setup(r => r.GetAllByCategoryAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync([existing]);

        var request = new UpdateResultRequest("101", DateTime.UtcNow.AddHours(-1), "Corrección de tiempo");
        await BuildService().UpdateAsync(1, 10, request, editorId: 3);

        _audits.Verify(a => a.AddAsync(It.Is<ResultAudit>(x => x.CampoModificado == "TiempoLlegada"), It.IsAny<CancellationToken>()), Times.Once);
        _audits.Verify(a => a.AddAsync(It.Is<ResultAudit>(x => x.CampoModificado == "Dorsal"), It.IsAny<CancellationToken>()), Times.Never);
        // Antes se evitaba llamar a ExistsByRunnerAsync cuando el dorsal no cambiaba
        // (runner.Id == result.RunnerId). TryResolveDorsalAsync (F2/F3) ya no distingue
        // ese caso: siempre pregunta, excluyéndose a sí mismo vía excludeResultId — el
        // resultado es el mismo (no hay duplicado), pero la llamada ahora sí ocurre.
        _results.Verify(r => r.ExistsByRunnerAsync(1, 5, 10, It.IsAny<CancellationToken>()), Times.Once);
        _results.Verify(r => r.GetAllByCategoryAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once);
        _dashboardNotifier.Verify(n => n.NotifyResultsChangedAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Antes: dorsal ya asignado a otro corredor lanzaba ConflictException (409) y
    // abortaba sin guardar nada. Ahora (F2): el resultado que intenta el dorsal
    // duplicado, y el que ya lo tenía, quedan ambos en Controversia — sin excepción,
    // sin aplicar el dorsal nuevo. Ver ResultDisputeTests.cs para la cobertura
    // completa del detector; este test solo confirma que el camino de UpdateAsync
    // que antes tiraba 409 ahora no tira nada.
    [Fact]
    public async Task Update_DorsalDeOtroCorredorYaTieneResultado_QuedaEnControversiaSinLanzar()
    {
        RaceExists();
        var existing = new Result
        {
            Id = 10, RaceId = 1, RunnerId = 5, Dorsal = "101",
            TiempoLlegada = DateTime.UtcNow.AddHours(-2), CategoryId = 2, CapturistaId = 9
        };
        _results.Setup(r => r.GetByIdAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var otroCorredor = new Runner { Id = 99, RaceId = 1, Dorsal = "202", CategoryId = 3, Nombre = "Beto" };
        _runners.Setup(r => r.GetByDorsalAsync(1, "202", It.IsAny<CancellationToken>())).ReturnsAsync(otroCorredor);
        _results.Setup(r => r.ExistsByRunnerAsync(1, 99, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var otroResultado = new Result
        {
            Id = 50, RaceId = 1, RunnerId = 99, Dorsal = "202",
            TiempoLlegada = DateTime.UtcNow.AddHours(-1), CategoryId = 3, CapturistaId = 9, Estado = ResultEstado.Valido
        };
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(otroResultado);
        _results.Setup(r => r.GetAllByCategoryAsync(1, 3, It.IsAny<CancellationToken>())).ReturnsAsync([otroResultado]);

        var request = new UpdateResultRequest("202", DateTime.UtcNow.AddHours(-1), "Corrigiendo dorsal");

        var dto = await BuildService().UpdateAsync(1, 10, request, editorId: 3);

        Assert.Equal(ResultEstado.Controversia, dto.Estado);
        Assert.Equal(DisputeMotivo.DorsalDuplicado, dto.DisputeMotivo);
        Assert.Equal("202", dto.DorsalPropuesto);
        Assert.Equal("101", existing.Dorsal); // el dorsal duplicado nunca se aplica
        _results.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // design.md Decisión 4 / Write-Path Risk Callout (tasks.md): RecalculatePositionsAsync
    // ordenaba solo por TiempoLlegada, sin desempate — dos resultados con el mismo
    // instante recibían un Posicion arbitrario y no reproducible (dependiente del orden
    // que EF devolviera esa corrida en particular). El desempate (TiempoLlegada, Id) debe
    // coincidir EXACTO con el que usa GetPlacingCountsAsync (ResultRepository) para que la
    // Posicion guardada nunca contradiga el placing derivado que ve el enlace público.
    // El mock devuelve el Id más alto PRIMERO a propósito: si el código solo preservara el
    // orden de entrada en vez de ordenar de verdad, este test lo detectaría.
    [Fact]
    public async Task Update_ResultadosConTiempoEmpatado_DesempataPorIdAscendente()
    {
        RaceExists();
        var tiempoEmpatado = DateTime.UtcNow.AddHours(-1);

        var existing = new Result
        {
            Id = 20, RaceId = 1, RunnerId = 5, Dorsal = "101",
            TiempoLlegada = DateTime.UtcNow.AddHours(-2), CategoryId = 2, CapturistaId = 9
        };
        var otroEmpatado = new Result
        {
            Id = 10, RaceId = 1, RunnerId = 6, Dorsal = "102",
            TiempoLlegada = tiempoEmpatado, CategoryId = 2, CapturistaId = 9
        };
        _results.Setup(r => r.GetByIdAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var runner = new Runner { Id = 5, RaceId = 1, Dorsal = "101", CategoryId = 2, Nombre = "Juan" };
        _runners.Setup(r => r.GetByDorsalAsync(1, "101", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        CategoryEnCurso(2);

        // Orden de entrada deliberadamente invertido (Id 20 antes que Id 10) — si
        // RecalculatePositionsAsync no ordenara de verdad por (TiempoLlegada, Id), este
        // test pasaría por casualidad con el orden "correcto" al revés.
        _results.Setup(r => r.GetAllByCategoryAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing, otroEmpatado]);

        var request = new UpdateResultRequest("101", tiempoEmpatado, "Empate de prueba");
        await BuildService().UpdateAsync(1, 20, request, editorId: 3);

        // Id 10 (menor) debe quedar antes que Id 20 (mayor) en el empate.
        Assert.Equal(1, otroEmpatado.Posicion);
        Assert.Equal(2, existing.Posicion);
    }

    [Fact]
    public async Task Update_TiempoFuturo_LanzaValidationException()
    {
        RaceExists();
        var existing = new Result
        {
            Id = 10, RaceId = 1, RunnerId = 5, Dorsal = "101",
            TiempoLlegada = DateTime.UtcNow.AddHours(-2), CategoryId = 2, CapturistaId = 9
        };
        _results.Setup(r => r.GetByIdAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new UpdateResultRequest("101", DateTime.UtcNow.AddDays(2), "Prueba");

        await Assert.ThrowsAsync<ValidationException>(
            () => BuildService().UpdateAsync(1, 10, request, editorId: 3));

        _runners.Verify(r => r.GetByDorsalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
