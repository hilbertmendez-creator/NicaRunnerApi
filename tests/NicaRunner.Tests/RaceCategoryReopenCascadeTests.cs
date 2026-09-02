using Moq;
using NicaRunner.Application.Auditing;
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
    private readonly Mock<IAuditService> _audit = new();

    // Por defecto toda categoría tiene corredores inscritos: estos tests son sobre
    // transiciones de estado, no sobre el gate de inscripción de StartAsync. Va en el
    // constructor y no en BuildService() para que un test que quiera probar el gate pueda
    // sobreescribirlo — en Moq gana el último Setup, y BuildService() corre después.
    public RaceCategoryReopenCascadeTests() =>
        _runners
            .Setup(r => r.ExistsByCategoryInRaceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    private const int AdminId = 1;

    private RaceCategoryService BuildService() =>
        new(_raceCategories.Object, _categories.Object, _races.Object, _runners.Object, _resultService.Object, _audit.Object);

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
