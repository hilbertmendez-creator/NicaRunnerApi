using Moq;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class CategoryDisputeCascadeTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _notifier = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();

    private const int AdminId = 1;

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object, _notifier.Object, _raceCategories.Object);

    [Fact]
    public async Task ResolvePendingCategoryDisputesAsync_ResuelveDisputaSinSalida_CuandoLaCategoriaYaEstaEnCurso()
    {
        var target = new Result { Id = 40, RaceId = 1, Estado = ResultEstado.Controversia, DisputeMotivo = Domain.Entities.DisputeMotivo.CategoriaSinSalida, DorsalPropuesto = "105", Posicion = 0, TiempoLlegada = DateTime.UtcNow };
        _results.Setup(r => r.GetDisputedByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([target]);
        _runners.Setup(r => r.GetByDorsalAsync(1, "105", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Runner { Id = 7, RaceId = 1, Nombre = "Corredor", Dorsal = "105", CategoryId = 5 });
        _results.Setup(r => r.ExistsByRunnerAsync(1, 7, 40, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RaceCategory { CategoryId = 5, Estado = RaceCategoryStatus.EnCurso });
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var resolved = await BuildService().ResolvePendingCategoryDisputesAsync(1, 5, AdminId, "corregido", default);

        Assert.Equal(1, resolved);
        Assert.Equal(ResultEstado.Valido, target.Estado);
        Assert.Equal("105", target.Dorsal);
        Assert.Equal(7, target.RunnerId);
        Assert.Equal(5, target.CategoryId);
        Assert.Null(target.DorsalPropuesto);
        Assert.Null(target.DisputeMotivo);
    }

    [Fact]
    public async Task ResolvePendingCategoryDisputesAsync_IgnoraDisputasDeOtraCategoria()
    {
        var target = new Result { Id = 40, RaceId = 1, Estado = ResultEstado.Controversia, DisputeMotivo = Domain.Entities.DisputeMotivo.CategoriaSinSalida, DorsalPropuesto = "105", TiempoLlegada = DateTime.UtcNow };
        _results.Setup(r => r.GetDisputedByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([target]);
        _runners.Setup(r => r.GetByDorsalAsync(1, "105", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Runner { Id = 7, RaceId = 1, Nombre = "Corredor", Dorsal = "105", CategoryId = 9 }); // otra categoría, no la 5

        var resolved = await BuildService().ResolvePendingCategoryDisputesAsync(1, 5, AdminId, "razon", default);

        Assert.Equal(0, resolved);
        Assert.Equal(ResultEstado.Controversia, target.Estado);
    }

    [Fact]
    public async Task ResolvePendingCategoryDisputesAsync_SiElDorsalYaFueTomado_QuedaEnNuevaDisputa()
    {
        var target = new Result { Id = 40, RaceId = 1, Estado = ResultEstado.Controversia, DisputeMotivo = Domain.Entities.DisputeMotivo.CategoriaSinSalida, DorsalPropuesto = "105", TiempoLlegada = DateTime.UtcNow };
        var existing = new Result { Id = 20, RaceId = 1, Dorsal = "105", RunnerId = 7, CategoryId = 5, Estado = ResultEstado.Valido, TiempoLlegada = DateTime.UtcNow.AddMinutes(-1) };
        _results.Setup(r => r.GetDisputedByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([target]);
        _runners.Setup(r => r.GetByDorsalAsync(1, "105", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Runner { Id = 7, RaceId = 1, Nombre = "Corredor", Dorsal = "105", CategoryId = 5 });
        _results.Setup(r => r.ExistsByRunnerAsync(1, 7, 40, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(1, 7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync([existing]);

        var resolved = await BuildService().ResolvePendingCategoryDisputesAsync(1, 5, AdminId, "razon", default);

        Assert.Equal(0, resolved);
        Assert.Equal(ResultEstado.Controversia, target.Estado);
        Assert.Equal(Domain.Entities.DisputeMotivo.DorsalDuplicado, target.DisputeMotivo);
    }

    [Fact]
    public async Task ResolvePendingCategoryDisputesAsync_IgnoraDorsalDuplicado()
    {
        var target = new Result { Id = 40, RaceId = 1, Estado = ResultEstado.Controversia, DisputeMotivo = Domain.Entities.DisputeMotivo.DorsalDuplicado, DorsalPropuesto = "105", DisputeGroupId = 39, TiempoLlegada = DateTime.UtcNow };
        _results.Setup(r => r.GetDisputedByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([target]);

        var resolved = await BuildService().ResolvePendingCategoryDisputesAsync(1, 5, AdminId, "razon", default);

        Assert.Equal(0, resolved);
        Assert.Equal(ResultEstado.Controversia, target.Estado);
        _runners.Verify(r => r.GetByDorsalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
