using Moq;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceCategoryStartCorrectionTests
{
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultService> _resultService = new();

    private const int AdminId = 1;

    private RaceCategoryService BuildService() =>
        new(_raceCategories.Object, _categories.Object, _races.Object, _runners.Object, _resultService.Object);

    private RaceCategory Setup(RaceCategoryStatus estado = RaceCategoryStatus.Planeada)
    {
        var category = new Category { Id = 5, Codigo = "5K", NombreCategoria = "5K", Distancia = 5, EdadMinima = 0, EdadMaxima = 99, Orden = 1 };
        var raceCategory = new RaceCategory { Id = 1, RaceId = 1, CategoryId = 5, Estado = estado, Category = category };
        var race = new Race { Id = 1, Nombre = "C", JoinCode = "X", Estado = RaceStatus.EnCurso };

        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(raceCategory);
        _raceCategories.Setup(rc => rc.GetAssociationsByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([raceCategory]);

        return raceCategory;
    }

    [Fact]
    public async Task CorrectStartAsync_CategoriaPlaneada_LaArrancaConLaHoraCorregida()
    {
        var raceCategory = Setup();
        var startUtc = DateTime.UtcNow.AddHours(-2);
        var request = new CorrectCategoryStartRequest(startUtc, "Se nos pasó arrancarla a tiempo");

        await BuildService().CorrectStartAsync(1, 5, request, AdminId);

        Assert.Equal(RaceCategoryStatus.EnCurso, raceCategory.Estado);
        Assert.Equal(startUtc, raceCategory.StartUtc);
        Assert.Equal(AdminId, raceCategory.StartedByUserId);
        Assert.Equal(StartClockOrigen.CorreccionAdmin, raceCategory.StartOrigen);
    }

    [Fact]
    public async Task CorrectStartAsync_DisparaLaCascadaDeDisputasDeLaCategoria()
    {
        Setup();
        var request = new CorrectCategoryStartRequest(DateTime.UtcNow.AddHours(-1), "razon");

        await BuildService().CorrectStartAsync(1, 5, request, AdminId);

        _resultService.Verify(
            s => s.ResolvePendingCategoryDisputesAsync(1, 5, AdminId, "razon", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CorrectStartAsync_CategoriaYaEnCurso_LanzaConflict()
    {
        Setup(estado: RaceCategoryStatus.EnCurso);
        var request = new CorrectCategoryStartRequest(DateTime.UtcNow.AddHours(-1), "razon");

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().CorrectStartAsync(1, 5, request, AdminId));
    }

    [Fact]
    public async Task CorrectStartAsync_HoraFutura_LanzaValidation()
    {
        Setup();
        var request = new CorrectCategoryStartRequest(DateTime.UtcNow.AddMinutes(10), "razon");

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().CorrectStartAsync(1, 5, request, AdminId));
    }

    [Fact]
    public async Task CorrectStartAsync_CategoriaNoAsignada_LanzaNotFound()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "C", JoinCode = "X" });
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync((RaceCategory?)null);
        var request = new CorrectCategoryStartRequest(DateTime.UtcNow.AddHours(-1), "razon");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().CorrectStartAsync(1, 99, request, AdminId));
    }
}
