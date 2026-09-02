using Moq;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

/// <summary>
/// Una categoría sin corredores inscritos no puede arrancar: su cero sería el origen de
/// tiempos que nadie puede reclamar, y el juez recién lo descubriría al asignar el primer
/// dorsal y encontrarse sin corredor.
/// </summary>
public class RaceCategoryStartRunnersGateTests
{
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultService> _resultService = new();
    private readonly Mock<IAuditService> _audit = new();

    private const int JudgeId = 42;

    private RaceCategoryService BuildService() =>
        new(_raceCategories.Object, _categories.Object, _races.Object, _runners.Object,
            _resultService.Object, _audit.Object);

    private static RaceCategory Assoc(int categoryId) => new()
    {
        Id = categoryId * 100,
        RaceId = 1,
        CategoryId = categoryId,
        Estado = RaceCategoryStatus.Planeada,
        Category = new Category { Id = categoryId, Codigo = $"C{categoryId}", NombreCategoria = $"Cat {categoryId}" }
    };

    private void Setup(params RaceCategory[] associations)
    {
        var race = new Race { Id = 1, Nombre = "Carrera", JoinCode = "ABC123" };
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories
            .Setup(rc => rc.GetAssociationsByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(associations.ToList());
        _raceCategories
            .Setup(rc => rc.GetAssociationsByIdsAsync(1, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, IReadOnlyCollection<int> ids, CancellationToken _) =>
                associations.Where(a => ids.Contains(a.CategoryId)).ToList());
    }

    private void ConCorredores(params int[] categoryIds)
    {
        _runners
            .Setup(r => r.ExistsByCategoryInRaceAsync(1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, int categoryId, CancellationToken _) => categoryIds.Contains(categoryId));
    }

    [Fact]
    public async Task StartAsync_CategoriaSinCorredores_LanzaConflictYNoArranca()
    {
        var a = Assoc(3);
        Setup(a);
        ConCorredores();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([3]), JudgeId));

        Assert.Contains("corredores inscritos", ex.Message);
        Assert.Equal(RaceCategoryStatus.Planeada, a.Estado);
        Assert.Null(a.StartUtc);
        _raceCategories.Verify(rc => rc.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // El disparo es atómico: si UNA de las categorías del balazo está vacía, no sale
    // ninguna. Dejar salir a la mitad rompería el "mismo balazo, mismo cero".
    [Fact]
    public async Task StartAsync_UnaDeVariasSinCorredores_NoArrancaNinguna()
    {
        var conGente = Assoc(3);
        var vacia = Assoc(7);
        Setup(conGente, vacia);
        ConCorredores(3);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([3, 7]), JudgeId));

        Assert.Contains("Cat 7", ex.Message);
        Assert.DoesNotContain("Cat 3", ex.Message);
        Assert.Equal(RaceCategoryStatus.Planeada, conGente.Estado);
        Assert.Null(conGente.StartUtc);
    }

    [Fact]
    public async Task StartAsync_TodasConCorredores_Arranca()
    {
        var a = Assoc(3);
        Setup(a);
        ConCorredores(3);

        await BuildService().StartAsync(1, new CategoryTransitionRequest([3]), JudgeId);

        Assert.Equal(RaceCategoryStatus.EnCurso, a.Estado);
        Assert.NotNull(a.StartUtc);
    }

    // El gate corre DESPUÉS del replay idempotente: un reintento offline de una salida que
    // ya se aplicó no vuelve a validar nada, devuelve lo que el cliente no llegó a recibir.
    // Sin este test, mover el chequeo más arriba rompería el reintento sin que nadie lo note.
    [Fact]
    public async Task StartAsync_ReintentoConLaMismaKey_NoRevalidaCorredores()
    {
        var a = Assoc(3);
        a.Estado = RaceCategoryStatus.EnCurso;
        a.StartIdempotencyKey = "key-1";
        Setup(a);
        ConCorredores(); // todos los corredores desaparecieron: igual tiene que replayear

        var result = await BuildService().StartAsync(
            1, new CategoryTransitionRequest([3], IdempotencyKey: "key-1"), JudgeId);

        Assert.Single(result);
        Assert.Equal(RaceCategoryStatus.EnCurso, a.Estado);
    }
}
