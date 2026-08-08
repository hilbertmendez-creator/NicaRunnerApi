using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class ResultVoidTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _notifier = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();

    private const int Author = 42;
    private const int OtherJudge = 43;
    private const int AdminId = 1;

    public ResultVoidTests()
    {
        // VoidAsync's ToDto call ahora enhebra el lookup de StartUtc por categoría
        // (Task 5) para poblar ElapsedMillis igual que el resto de los endpoints.
        // Sin este setup, GetAssociationsByRaceAsync sin mockear devuelve null y
        // GetStartUtcByCategoryIdAsync revienta con NullReferenceException al armar
        // el diccionario — mismo problema ya documentado en ResultDisputeTests.
        _raceCategories.Setup(rc => rc.GetAssociationsByRaceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RaceCategory>());
    }

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object, _notifier.Object, _raceCategories.Object);

    private Result Setup(RaceStatus raceStatus = RaceStatus.EnCurso, int capturistaId = Author, int? categoryId = 5, int? runnerId = null, string? dorsal = null)
    {
        var result = new Result { Id = 1, RaceId = 1, CapturistaId = capturistaId, CategoryId = categoryId, RunnerId = runnerId, Dorsal = dorsal, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow };
        _results.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "C", JoinCode = "X", Estado = raceStatus });
        if (categoryId is { } cid)
            _results.Setup(r => r.GetAllByCategoryAsync(1, cid, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return result;
    }

    [Fact]
    public async Task VoidAsync_AutorPropiaCaptura_LaAnula()
    {
        var result = Setup();
        result.Posicion = 3; // simula un resultado ya rankeado antes de deshacerlo

        var dto = await BuildService().VoidAsync(1, 1, new VoidResultRequest("toque fantasma"), Author, isAdmin: false);

        Assert.Equal(ResultEstado.Anulado, dto.Estado);
        Assert.Equal(ResultEstado.Anulado, result.Estado);
        // Un resultado Anulado no puede seguir mostrando la posición que tenía cuando
        // todavía era Valido — si esto falla, el podio muestra un dato descartado.
        Assert.Equal(0, result.Posicion);
    }

    [Fact]
    public async Task VoidAsync_OtroJuezNoAdmin_LanzaForbidden()
    {
        Setup();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            BuildService().VoidAsync(1, 1, new VoidResultRequest("no es mío"), OtherJudge, isAdmin: false));
    }

    [Fact]
    public async Task VoidAsync_Admin_PuedeAnularCualquierCaptura()
    {
        var result = Setup(capturistaId: OtherJudge);

        var dto = await BuildService().VoidAsync(1, 1, new VoidResultRequest("corrección administrativa"), AdminId, isAdmin: true);

        Assert.Equal(ResultEstado.Anulado, dto.Estado);
    }

    [Fact]
    public async Task VoidAsync_AutorConCarreraTerminada_LanzaForbidden()
    {
        Setup(raceStatus: RaceStatus.Terminada);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            BuildService().VoidAsync(1, 1, new VoidResultRequest("tarde"), Author, isAdmin: false));
    }

    [Fact]
    public async Task VoidAsync_AdminConCarreraTerminada_FuncionaIgual()
    {
        var result = Setup(raceStatus: RaceStatus.Terminada, capturistaId: OtherJudge);

        var dto = await BuildService().VoidAsync(1, 1, new VoidResultRequest("corrección tardía"), AdminId, isAdmin: true);

        Assert.Equal(ResultEstado.Anulado, dto.Estado);
    }

    [Fact]
    public async Task VoidAsync_SinRazon_LanzaValidation()
    {
        Setup();

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().VoidAsync(1, 1, new VoidResultRequest(""), Author, isAdmin: false));
    }

    [Fact]
    public async Task VoidAsync_LiberaRunnerIdYDorsal()
    {
        // Postgres real (no este mock): IX_Results_RaceId_RunnerId es único por
        // (RaceId, RunnerId) sin importar Estado. Si un Anulado retuviera su
        // RunnerId, ni una nueva captura de ese dorsal ni una resolución de
        // disputa podrían asignárselo a otro resultado sin un 500 por violación
        // de UK — bug real encontrado en la verificación manual de PR2a Task 8.
        var result = Setup(runnerId: 7, dorsal: "701");

        var dto = await BuildService().VoidAsync(1, 1, new VoidResultRequest("toque fantasma"), Author, isAdmin: false);

        Assert.Null(result.RunnerId);
        Assert.Null(result.Dorsal);
        Assert.Null(dto.Dorsal);
    }

    [Fact]
    public async Task VoidAsync_RecalculaPosicionesDeLaCategoriaAfectada()
    {
        var result = Setup();

        await BuildService().VoidAsync(1, 1, new VoidResultRequest("toque fantasma"), Author, isAdmin: false);

        _results.Verify(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
