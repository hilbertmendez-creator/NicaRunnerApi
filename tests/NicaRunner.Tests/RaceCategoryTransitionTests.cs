using Moq;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceCategoryTransitionTests
{
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();

    private const int JudgeId = 42;

    private RaceCategoryService BuildService() =>
        new(_raceCategories.Object, _categories.Object, _races.Object, _runners.Object);

    private static RaceCategory Assoc(int categoryId, RaceCategoryStatus estado = RaceCategoryStatus.Planeada) =>
        new()
        {
            Id = categoryId * 100,
            RaceId = 1,
            CategoryId = categoryId,
            Estado = estado,
            Category = new Category { Id = categoryId, Codigo = $"C{categoryId}", NombreCategoria = $"Cat {categoryId}" }
        };

    /// <summary>
    /// Arma el repo para que las lecturas por id y por carrera devuelvan LAS MISMAS
    /// instancias, igual que hace la resolución de identidad de EF. Sin esto el test
    /// no probaría la derivación de Race.Estado sobre las entidades ya mutadas.
    /// </summary>
    private Race Setup(params RaceCategory[] associations)
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
        return race;
    }

    [Fact]
    public async Task StartAsync_VariasCategorias_CompartenExactamenteElMismoStartUtc()
    {
        var a = Assoc(3);
        var b = Assoc(7);
        var c = Assoc(9);
        Setup(a, b, c);

        await BuildService().StartAsync(1, new CategoryTransitionRequest([3, 7, 9]), JudgeId);

        Assert.NotNull(a.StartUtc);
        Assert.Equal(a.StartUtc, b.StartUtc);
        Assert.Equal(a.StartUtc, c.StartUtc);
    }

    [Fact]
    public async Task StartAsync_MarcaEnCursoYAtribuyeAlJuez()
    {
        var a = Assoc(3);
        Setup(a);

        await BuildService().StartAsync(1, new CategoryTransitionRequest([3]), JudgeId);

        Assert.Equal(RaceCategoryStatus.EnCurso, a.Estado);
        Assert.Equal(JudgeId, a.StartedByUserId);
    }

    [Fact]
    public async Task StartAsync_DerivaRaceEstadoAEnCurso()
    {
        var a = Assoc(3);
        var b = Assoc(7);
        var race = Setup(a, b);

        await BuildService().StartAsync(1, new CategoryTransitionRequest([3]), JudgeId);

        Assert.Equal(RaceStatus.EnCurso, race.Estado);
        Assert.Equal(a.StartUtc, race.RaceStartUtc);
    }

    [Fact]
    public async Task StartAsync_UnaYaArrancada_LanzaConflictYNoTocaLasOtras()
    {
        var a = Assoc(3, RaceCategoryStatus.EnCurso);
        var b = Assoc(7);
        Setup(a, b);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([3, 7]), JudgeId));

        // Atómico: o arrancan las dos o no arranca ninguna.
        Assert.Equal(RaceCategoryStatus.Planeada, b.Estado);
        Assert.Null(b.StartUtc);
        _raceCategories.Verify(rc => rc.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // El chequeo de conflicto es `Estado != Planeada`, no `Estado == EnCurso`: una
    // categoría ya Terminada tampoco puede "arrancar" de nuevo por esta vía (para eso
    // está Reopen). Sin este test, angostar el chequeo a solo EnCurso pasaría inadvertido.
    [Fact]
    public async Task StartAsync_UnaYaTerminada_LanzaConflict()
    {
        var a = Assoc(3, RaceCategoryStatus.Terminada);
        Setup(a);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([3]), JudgeId));

        _raceCategories.Verify(rc => rc.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_CategoriaNoAsignadaALaCarrera_LanzaNotFound()
    {
        Setup(Assoc(3));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([3, 99]), JudgeId));
    }

    [Fact]
    public async Task StartAsync_ListaVacia_LanzaValidation()
    {
        Setup(Assoc(3));

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([]), JudgeId));
    }

    [Fact]
    public async Task StartAsync_IdsRepetidos_LosTrataUnaSolaVez()
    {
        var a = Assoc(3);
        Setup(a);

        await BuildService().StartAsync(1, new CategoryTransitionRequest([3, 3]), JudgeId);

        Assert.Equal(RaceCategoryStatus.EnCurso, a.Estado);
    }

    [Fact]
    public async Task StartAsync_CarreraInexistente_LanzaNotFound()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Race?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([3]), JudgeId));
    }
}
