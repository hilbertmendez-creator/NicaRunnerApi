using Moq;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

/// <summary>
/// Salida en falso. Es la única transición de categoría que DESTRUYE datos —borra el cero y
/// anula las llegadas medidas contra él—, así que lo que estos tests protegen no es que
/// funcione sino que no se lleve puesto más de lo que le corresponde.
/// </summary>
public class RaceCategoryResetStartTests
{
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultService> _resultService = new();
    private readonly Mock<IAuditService> _audit = new();

    private const int AdminId = 7;
    private const string Razon = "Salida en falso: se largó antes del disparo.";

    private RaceCategoryService BuildService() =>
        new(_raceCategories.Object, _categories.Object, _races.Object, _runners.Object,
            _resultService.Object, _audit.Object);

    private static RaceCategory Assoc(int categoryId, RaceCategoryStatus estado) => new()
    {
        Id = categoryId * 100,
        RaceId = 1,
        CategoryId = categoryId,
        Estado = estado,
        StartUtc = estado == RaceCategoryStatus.Planeada ? null : DateTime.UtcNow.AddMinutes(-5),
        StartedByUserId = estado == RaceCategoryStatus.Planeada ? null : 42,
        StartOrigen = estado == RaceCategoryStatus.Planeada ? null : StartClockOrigen.Cliente,
        StartOffsetConfianzaMs = estado == RaceCategoryStatus.Planeada ? null : 250,
        StartIdempotencyKey = estado == RaceCategoryStatus.Planeada ? null : $"key-{categoryId}",
        Category = new Category { Id = categoryId, Codigo = $"C{categoryId}", NombreCategoria = $"Cat {categoryId}" }
    };

    private Race Setup(params RaceCategory[] associations)
    {
        var race = new Race { Id = 1, Nombre = "Carrera", JoinCode = "ABC123", Estado = RaceStatus.EnCurso };
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories
            .Setup(rc => rc.GetAssociationsByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(associations.ToList());
        _raceCategories
            .Setup(rc => rc.GetAssociationsByIdsAsync(1, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, IReadOnlyCollection<int> ids, CancellationToken _) =>
                associations.Where(a => ids.Contains(a.CategoryId)).ToList());
        _resultService
            .Setup(s => s.VoidForResetAsync(1, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<bool>(),
                AdminId, Razon, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        return race;
    }

    [Fact]
    public async Task ResetStartAsync_BorraElCeroCompletoYDevuelveLaCategoriaAPlaneada()
    {
        var a = Assoc(3, RaceCategoryStatus.EnCurso);
        Setup(a);

        var result = await BuildService().ResetStartAsync(1, new ResetCategoryStartRequest([3], Razon), AdminId);

        Assert.Equal(RaceCategoryStatus.Planeada, a.Estado);
        Assert.Null(a.StartUtc);
        Assert.Null(a.StartedByUserId);
        Assert.Null(a.StartOrigen);
        Assert.Null(a.StartOffsetConfianzaMs);
        Assert.Equal(3, result.LlegadasAnuladas);
    }

    // Si la key sobreviviera, un reintento tardío de la salida anulada entraría por
    // TryGetIdempotentReplay y devolvería "ya arrancó" sin arrancar nada.
    [Fact]
    public async Task ResetStartAsync_LimpiaLaKeyDeIdempotenciaDeLaSalidaAnulada()
    {
        var a = Assoc(3, RaceCategoryStatus.EnCurso);
        Setup(a);

        await BuildService().ResetStartAsync(1, new ResetCategoryStartRequest([3], Razon), AdminId);

        Assert.Null(a.StartIdempotencyKey);
    }

    // Reiniciar UNA categoría no puede tocar las capturas sin dorsal: podrían ser de
    // cualquiera de las otras que siguen corriendo bien.
    [Fact]
    public async Task ResetStartAsync_NoAnulaLasCapturasSinCategoria()
    {
        Setup(Assoc(3, RaceCategoryStatus.EnCurso), Assoc(7, RaceCategoryStatus.EnCurso));

        await BuildService().ResetStartAsync(1, new ResetCategoryStartRequest([3], Razon), AdminId);

        _resultService.Verify(s => s.VoidForResetAsync(
            1, It.Is<IReadOnlyCollection<int>>(ids => ids.Single() == 3), false,
            AdminId, Razon, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetStartAsync_CategoriaNoEnCurso_LanzaConflict()
    {
        var terminada = Assoc(3, RaceCategoryStatus.Terminada);
        Setup(terminada);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().ResetStartAsync(1, new ResetCategoryStartRequest([3], Razon), AdminId));

        Assert.Contains("Cat 3", ex.Message);
        Assert.Equal(RaceCategoryStatus.Terminada, terminada.Estado);
        _resultService.Verify(s => s.VoidForResetAsync(
            It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetStartAsync_DerivaRaceEstadoAPlaneadaCuandoNoQuedaNingunaArrancada()
    {
        var a = Assoc(3, RaceCategoryStatus.EnCurso);
        var race = Setup(a);

        await BuildService().ResetStartAsync(1, new ResetCategoryStartRequest([3], Razon), AdminId);

        Assert.Equal(RaceStatus.Planeada, race.Estado);
        Assert.Null(race.RaceStartUtc);
    }

    // Reinicio de carrera completa: alcanza también a las Terminada, porque una salida en
    // falso se descubre a veces después de que alguien ya cerró la categoría.
    [Fact]
    public async Task RestartRaceAsync_AlcanzaCategoriasTerminadaYCapturasSinDorsal()
    {
        var enCurso = Assoc(3, RaceCategoryStatus.EnCurso);
        var terminada = Assoc(7, RaceCategoryStatus.Terminada);
        var nuncaArranco = Assoc(9, RaceCategoryStatus.Planeada);
        var race = Setup(enCurso, terminada, nuncaArranco);

        var result = await BuildService().RestartRaceAsync(1, new RestartRaceRequest(Razon), AdminId);

        Assert.Equal(RaceCategoryStatus.Planeada, enCurso.Estado);
        Assert.Equal(RaceCategoryStatus.Planeada, terminada.Estado);
        Assert.Null(terminada.ClosedUtc);
        Assert.Equal(RaceStatus.Planeada, race.Estado);
        Assert.Equal(2, result.Categories.Count); // la que nunca arrancó no se toca

        _resultService.Verify(s => s.VoidForResetAsync(
            1, It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 2 && ids.Contains(3) && ids.Contains(7)),
            true, AdminId, Razon, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestartRaceAsync_NingunaCategoriaArrancada_LanzaConflict()
    {
        Setup(Assoc(3, RaceCategoryStatus.Planeada));

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().RestartRaceAsync(1, new RestartRaceRequest(Razon), AdminId));
    }

    // Con cero llegadas registradas no hay ningún ResultAudit, así que sin esta entrada el
    // reinicio no dejaría rastro en ningún lado.
    [Fact]
    public async Task ResetStartAsync_DejaEntradaEnAuditLogConLaRazon()
    {
        Setup(Assoc(3, RaceCategoryStatus.EnCurso));

        await BuildService().ResetStartAsync(1, new ResetCategoryStartRequest([3], Razon), AdminId);

        _audit.Verify(a => a.TrackChanges(
            AuditEntityTypes.Race, 1, AdminId,
            It.Is<IEnumerable<FieldChange>>(cs => cs.Single().ValorNuevo!.Contains(Razon))), Times.Once);
    }
}
