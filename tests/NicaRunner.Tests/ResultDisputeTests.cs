using Moq;
using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class ResultDisputeTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _notifier = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();

    private const int JudgeB = 43;

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object, _notifier.Object, _raceCategories.Object);

    private static Race EnCurso() => new()
    {
        Id = 1, Nombre = "C", JoinCode = "X", Estado = RaceStatus.EnCurso, RaceStartUtc = DateTime.UtcNow.AddHours(-1)
    };

    private static RaceCategory Assoc(int categoryId, RaceCategoryStatus estado, DateTime? startUtc = null) => new()
    {
        RaceId = 1, CategoryId = categoryId, Estado = estado, StartUtc = startUtc,
        Category = new Category { Id = categoryId, NombreCategoria = "5K" }
    };

    // ---- F2: dorsal duplicado, vía UpdateAsync (el camino común: dorsal asignado
    // después de capturar sin dorsal) ----

    [Fact]
    public async Task UpdateAsync_DorsalYaAsignadoAOtroResultado_AmbosQuedanEnControversia()
    {
        var race = EnCurso();
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assoc(5, RaceCategoryStatus.EnCurso, race.RaceStartUtc));

        var runner = new Runner { Id = 9, RaceId = 1, Dorsal = "105", CategoryId = 5 };
        _runners.Setup(r => r.GetByDorsalAsync(1, "105", It.IsAny<CancellationToken>())).ReturnsAsync(runner);

        var resultA = new Result { Id = 1, RaceId = 1, RunnerId = 9, Dorsal = "105", CategoryId = 5, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow.AddMinutes(-5) };
        var resultB = new Result { Id = 2, RaceId = 1, Dorsal = null, CategoryId = null, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow.AddMinutes(-3) };
        _results.Setup(r => r.GetByIdAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(resultB);
        _results.Setup(r => r.ExistsByRunnerAsync(1, 9, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(1, 9, It.IsAny<CancellationToken>())).ReturnsAsync(resultA);
        // RecalculatePositionsAsync(raceId, existing.CategoryId) corre como parte de F2 —
        // sin este mock, el repo mockeado (loose) devuelve null para la lista y el LINQ
        // .Where() explota con ArgumentNullException.
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync([resultA]);

        var dto = await BuildService().UpdateAsync(1, 2, new UpdateResultRequest("105", resultB.TiempoLlegada, "asignado"), JudgeB);

        Assert.Equal(ResultEstado.Controversia, dto.Estado);
        Assert.Equal("105", dto.DorsalPropuesto);
        Assert.Null(resultB.Dorsal); // NUNCA se aplica — sigue sin dorsal real
        Assert.Null(resultB.CategoryId);
        Assert.Equal(ResultEstado.Controversia, resultA.Estado); // el otro lado también
        Assert.NotNull(resultA.DisputeGroupId);
        Assert.Equal(resultA.DisputeGroupId, resultB.DisputeGroupId);
        Assert.Equal(DisputeMotivo.DorsalDuplicado, resultB.DisputeMotivo);
        _notifier.Verify(n => n.NotifyDisputeOpenedAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DorsalDuplicado_DevuelveHttp200NoExcepcion()
    {
        // El propio hecho de que este test compile y no espere una excepción prueba D8:
        // el llamado no lanza — el 200 lo decide el controller devolviendo el DTO.
        var race = EnCurso();
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assoc(5, RaceCategoryStatus.EnCurso, race.RaceStartUtc));
        var runner = new Runner { Id = 9, RaceId = 1, Dorsal = "105", CategoryId = 5 };
        _runners.Setup(r => r.GetByDorsalAsync(1, "105", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        var resultA = new Result { Id = 1, RaceId = 1, RunnerId = 9, Dorsal = "105", CategoryId = 5, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow.AddMinutes(-5) };
        var resultB = new Result { Id = 2, RaceId = 1, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow.AddMinutes(-3) };
        _results.Setup(r => r.GetByIdAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(resultB);
        _results.Setup(r => r.ExistsByRunnerAsync(1, 9, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(1, 9, It.IsAny<CancellationToken>())).ReturnsAsync(resultA);
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync([resultA]);

        var dto = await BuildService().UpdateAsync(1, 2, new UpdateResultRequest("105", resultB.TiempoLlegada, "asignado"), JudgeB);

        Assert.NotNull(dto); // no throw
    }

    // ---- F3: categoría sin salida / cerrada ----

    [Fact]
    public async Task UpdateAsync_CategoriaSinArrancar_QuedaEnControversiaConEseMotivo()
    {
        var race = EnCurso();
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assoc(5, RaceCategoryStatus.Planeada, startUtc: null));

        var runner = new Runner { Id = 9, RaceId = 1, Dorsal = "77", CategoryId = 5 };
        _runners.Setup(r => r.GetByDorsalAsync(1, "77", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        var result = new Result { Id = 1, RaceId = 1, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow };
        _results.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _results.Setup(r => r.ExistsByRunnerAsync(1, 9, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await BuildService().UpdateAsync(1, 1, new UpdateResultRequest("77", result.TiempoLlegada, "asignado"), JudgeB);

        Assert.Equal(ResultEstado.Controversia, dto.Estado);
        Assert.Equal(DisputeMotivo.CategoriaSinSalida, dto.DisputeMotivo);
        Assert.Equal("77", dto.DorsalPropuesto);
        Assert.Null(result.Dorsal);
        Assert.Null(result.DisputeGroupId); // sin contraparte — conflicto contra estado
        _notifier.Verify(n => n.NotifyDisputeOpenedAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CategoriaYaCerrada_QuedaEnControversiaConEseMotivo()
    {
        var race = EnCurso();
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assoc(5, RaceCategoryStatus.Terminada, race.RaceStartUtc));

        var runner = new Runner { Id = 9, RaceId = 1, Dorsal = "77", CategoryId = 5 };
        _runners.Setup(r => r.GetByDorsalAsync(1, "77", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        var result = new Result { Id = 1, RaceId = 1, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow };
        _results.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _results.Setup(r => r.ExistsByRunnerAsync(1, 9, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await BuildService().UpdateAsync(1, 1, new UpdateResultRequest("77", result.TiempoLlegada, "asignado"), JudgeB);

        Assert.Equal(ResultEstado.Controversia, dto.Estado);
        Assert.Equal(DisputeMotivo.CategoriaCerrada, dto.DisputeMotivo);
    }

    [Fact]
    public async Task UpdateAsync_CategoriaEnCurso_AsignaNormalmenteSinControversia()
    {
        var race = EnCurso();
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assoc(5, RaceCategoryStatus.EnCurso, race.RaceStartUtc));

        var runner = new Runner { Id = 9, RaceId = 1, Dorsal = "77", CategoryId = 5 };
        _runners.Setup(r => r.GetByDorsalAsync(1, "77", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        var result = new Result { Id = 1, RaceId = 1, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow };
        _results.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _results.Setup(r => r.ExistsByRunnerAsync(1, 9, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync([result]);

        var dto = await BuildService().UpdateAsync(1, 1, new UpdateResultRequest("77", result.TiempoLlegada, "asignado"), JudgeB);

        Assert.Equal(ResultEstado.Valido, dto.Estado);
        Assert.Equal("77", result.Dorsal);
        Assert.Equal(5, result.CategoryId);
        Assert.Null(dto.DisputeMotivo);
        _notifier.Verify(n => n.NotifyDisputeOpenedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Mismo detector, vía CreateAsync (dorsal provisto al capturar) ----

    [Fact]
    public async Task CreateAsync_ConDorsalYaAsignado_QuedaEnControversiaSinLanzar()
    {
        var race = EnCurso();
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Assoc(5, RaceCategoryStatus.EnCurso, race.RaceStartUtc));

        var runner = new Runner { Id = 9, RaceId = 1, Dorsal = "105", CategoryId = 5 };
        _runners.Setup(r => r.GetByDorsalAsync(1, "105", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.ExistsByRunnerAsync(1, 9, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var resultA = new Result { Id = 1, RaceId = 1, RunnerId = 9, Dorsal = "105", CategoryId = 5, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow.AddMinutes(-5) };
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(1, 9, It.IsAny<CancellationToken>())).ReturnsAsync(resultA);
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync([resultA]);

        var dto = await BuildService().CreateAsync(1, new CreateResultRequest("105"), JudgeB);

        Assert.Equal(ResultEstado.Controversia, dto.Estado);
        Assert.Equal(DisputeMotivo.DorsalDuplicado, dto.DisputeMotivo);
    }
}
