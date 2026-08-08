using Moq;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceCategoryReopenCascadeTests
{
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultService> _resultService = new();

    private const int AdminId = 1;

    private RaceCategoryService BuildService() =>
        new(_raceCategories.Object, _categories.Object, _races.Object, _runners.Object, _resultService.Object);

    private RaceCategory Setup()
    {
        var category = new Category { Id = 5, Codigo = "5K", NombreCategoria = "5K", Distancia = 5, EdadMinima = 0, EdadMaxima = 99, Orden = 1 };
        var raceCategory = new RaceCategory { Id = 1, RaceId = 1, CategoryId = 5, Estado = RaceCategoryStatus.Terminada, Category = category };
        var race = new Race { Id = 1, Nombre = "C", JoinCode = "X", Estado = RaceStatus.Terminada };

        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationsByIdsAsync(1, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([raceCategory]);
        _raceCategories.Setup(rc => rc.GetAssociationsByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([raceCategory]);

        return raceCategory;
    }

    [Fact]
    public async Task ReopenAsync_DisparaLaCascadaDeDisputasParaCadaCategoriaReabierta()
    {
        Setup();
        var request = new CategoryTransitionRequest(CategoryIds: [5]);

        await BuildService().ReopenAsync(1, request, AdminId);

        _resultService.Verify(
            s => s.ResolvePendingCategoryDisputesAsync(1, 5, AdminId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
