using Moq;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceCategoryClientClockTests
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
    public async Task StartAsync_SinCamposDeCliente_UsaRelojDelServidor()
    {
        var a = Assoc(3);
        Setup(a);
        var antes = DateTime.UtcNow;

        await BuildService().StartAsync(1, new CategoryTransitionRequest([3]), JudgeId);

        Assert.Equal(StartClockOrigen.Servidor, a.StartOrigen);
        Assert.Null(a.StartOffsetConfianzaMs);
        Assert.InRange(a.StartUtc!.Value, antes, DateTime.UtcNow);
    }

    [Fact]
    public async Task StartAsync_ConClienteYOffsetCalibrado_UsaOrigenCliente()
    {
        var a = Assoc(3);
        Setup(a);
        var startCliente = DateTime.UtcNow.AddMinutes(-1);

        await BuildService().StartAsync(
            1,
            new CategoryTransitionRequest([3], StartUtcCliente: startCliente, OffsetConfianzaMs: 320, CalibradoHaceMs: 45_000),
            JudgeId);

        Assert.Equal(StartClockOrigen.Cliente, a.StartOrigen);
        Assert.Equal(320, a.StartOffsetConfianzaMs);
        Assert.Equal(startCliente, a.StartUtc);
    }

    [Fact]
    public async Task StartAsync_ConClienteSinOffset_UsaOrigenClienteSinCalibrar()
    {
        var a = Assoc(3);
        Setup(a);
        var startCliente = DateTime.UtcNow.AddMinutes(-1);

        await BuildService().StartAsync(
            1,
            new CategoryTransitionRequest([3], StartUtcCliente: startCliente, OffsetConfianzaMs: null, CalibradoHaceMs: null),
            JudgeId);

        Assert.Equal(StartClockOrigen.ClienteSinCalibrar, a.StartOrigen);
        Assert.Null(a.StartOffsetConfianzaMs);
    }

    [Fact]
    public async Task StartAsync_VariasCategoriasConCliente_CompartenElMismoStartUtcCliente()
    {
        var a = Assoc(3);
        var b = Assoc(7);
        Setup(a, b);
        var startCliente = DateTime.UtcNow.AddMinutes(-1);

        await BuildService().StartAsync(
            1, new CategoryTransitionRequest([3, 7], StartUtcCliente: startCliente, OffsetConfianzaMs: 100), JudgeId);

        Assert.Equal(startCliente, a.StartUtc);
        Assert.Equal(startCliente, b.StartUtc);
    }

    [Fact]
    public async Task StartAsync_ClienteMasDeCincoMinutosEnElFuturo_LanzaValidation()
    {
        var a = Assoc(3);
        Setup(a);
        var futuro = DateTime.UtcNow.AddMinutes(6);

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([3], StartUtcCliente: futuro), JudgeId));
    }

    [Fact]
    public async Task StartAsync_ClienteDentroDeCincoMinutosEnElFuturo_NoLanza()
    {
        var a = Assoc(3);
        Setup(a);
        var futuroTolerado = DateTime.UtcNow.AddMinutes(4);

        await BuildService().StartAsync(1, new CategoryTransitionRequest([3], StartUtcCliente: futuroTolerado), JudgeId);

        Assert.Equal(futuroTolerado, a.StartUtc);
    }

    [Fact]
    public async Task StartAsync_ClienteMasDeDoceHorasViejo_LanzaValidation()
    {
        var a = Assoc(3);
        Setup(a);
        var viejo = DateTime.UtcNow.AddHours(-13);

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().StartAsync(1, new CategoryTransitionRequest([3], StartUtcCliente: viejo), JudgeId));
    }

    [Fact]
    public async Task StartAsync_ClienteDentroDeDoceHoras_NoLanza()
    {
        var a = Assoc(3);
        Setup(a);
        var recienToleable = DateTime.UtcNow.AddHours(-11);

        await BuildService().StartAsync(1, new CategoryTransitionRequest([3], StartUtcCliente: recienToleable), JudgeId);

        Assert.Equal(recienToleable, a.StartUtc);
    }
}
