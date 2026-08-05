using Moq;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceCategoryServiceTests
{
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();

    private RaceCategoryService BuildService() =>
        new(_raceCategories.Object, _categories.Object, _races.Object, _runners.Object);

    private static Race SomeRace(int id = 1) => new() { Id = id, Nombre = "Carrera", JoinCode = "ABC123" };

    [Fact]
    public async Task AssignAsync_CategoriaDelCatalogoNoAsignada_LaAsigna()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SomeRace());
        var category = new Category { Id = 5, Codigo = "JUV", NombreCategoria = "Juvenil" };
        _categories.Setup(c => c.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _raceCategories.Setup(rc => rc.IsSelectedAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await BuildService().AssignAsync(1, new AssignCategoryRequest(5));

        Assert.Equal("JUV", dto.Codigo);
        _raceCategories.Verify(rc => rc.SelectAsync(1, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignAsync_CarreraInexistente_LanzaNotFound()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Race?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => BuildService().AssignAsync(1, new AssignCategoryRequest(5)));
    }

    [Fact]
    public async Task AssignAsync_CategoriaInexistenteEnCatalogo_LanzaNotFound()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SomeRace());
        _categories.Setup(c => c.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => BuildService().AssignAsync(1, new AssignCategoryRequest(99)));
    }

    [Fact]
    public async Task AssignAsync_CategoriaYaAsignada_LanzaConflict()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SomeRace());
        _categories.Setup(c => c.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 5, NombreCategoria = "Juvenil" });
        _raceCategories.Setup(rc => rc.IsSelectedAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => BuildService().AssignAsync(1, new AssignCategoryRequest(5)));

        _raceCategories.Verify(rc => rc.SelectAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnassignAsync_SinCorredoresInscritos_Quita()
    {
        var association = new RaceCategory { RaceId = 1, CategoryId = 5 };
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(association);
        _runners.Setup(r => r.ExistsByCategoryInRaceAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await BuildService().UnassignAsync(1, 5);

        _raceCategories.Verify(rc => rc.Remove(association), Times.Once);
    }

    [Fact]
    public async Task UnassignAsync_ConCorredoresInscritosEnLaCarrera_LanzaConflict()
    {
        var association = new RaceCategory { RaceId = 1, CategoryId = 5 };
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(association);
        _runners.Setup(r => r.ExistsByCategoryInRaceAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => BuildService().UnassignAsync(1, 5));

        _raceCategories.Verify(rc => rc.Remove(It.IsAny<RaceCategory>()), Times.Never);
    }

    [Fact]
    public async Task UnassignAsync_NoAsignada_LanzaNotFound()
    {
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync((RaceCategory?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => BuildService().UnassignAsync(1, 5));
    }

    // ---------- ConfigureAsync (tasks.md 2.11 / 2.13) ----------

    [Fact]
    public async Task ConfigureAsync_AsociacionExistente_ActualizaCapacidadYPrecio()
    {
        var association = new RaceCategory { RaceId = 1, CategoryId = 5 };
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(association);
        _categories.Setup(c => c.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 5, Codigo = "JUV", NombreCategoria = "Juvenil" });

        var dto = await BuildService().ConfigureAsync(1, 5, new ConfigureRaceCategoryRequest(50, 250m));

        Assert.Equal(50, association.Capacidad);
        Assert.Equal(250m, association.Precio);
        Assert.Equal(50, dto.Capacidad);
        Assert.Equal(250m, dto.Precio);
        _raceCategories.Verify(rc => rc.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigureAsync_CapacidadNula_SePersisteComoSinLimite()
    {
        var association = new RaceCategory { RaceId = 1, CategoryId = 5, Capacidad = 20 };
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(association);
        _categories.Setup(c => c.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 5, Codigo = "JUV", NombreCategoria = "Juvenil" });

        var dto = await BuildService().ConfigureAsync(1, 5, new ConfigureRaceCategoryRequest(null, 100m));

        Assert.Null(association.Capacidad);
        Assert.Null(dto.Capacidad);
        Assert.Equal(100m, dto.Precio);
    }

    [Fact]
    public async Task ConfigureAsync_NoAsignada_LanzaNotFound()
    {
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync((RaceCategory?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().ConfigureAsync(1, 5, new ConfigureRaceCategoryRequest(50, 250m)));

        _raceCategories.Verify(rc => rc.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
