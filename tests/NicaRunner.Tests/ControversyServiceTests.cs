using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Controversies;
using NicaRunner.Application.Controversies.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class ControversyServiceTests
{
    private readonly Mock<IControversyRepository> _controversies = new();
    private readonly Mock<IRaceRepository> _races = new();

    private ControversyService BuildService() => new(_controversies.Object, _races.Object);

    private void RaceExists(int raceId = 1) =>
        _races.Setup(r => r.GetByIdAsync(raceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = raceId, Nombre = "Gran Fondo 2026", AdminId = 1 });

    private static Controversy Abierta(int id = 1, string dorsal = "101") => new()
    {
        Id = id,
        RaceId = 1,
        Dorsal = dorsal,
        Nombre = "Ana Pérez",
        Categoria = "Femenino 21K",
        TiempoChip = 6315.0,
        TiempoCaptura = 6318.3,
        TiempoCamara = 6316.1,
        Diferencia = 3.3,
        Estado = "Abierta",
    };

    [Fact]
    public async Task List_DevuelveDisputasMapeadas()
    {
        RaceExists();
        _controversies.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Abierta(1),
                new Controversy { Id = 2, RaceId = 1, Dorsal = "205", Nombre = "Luis", Categoria = "Libre 10K", Estado = "Resuelta" },
            ]);

        var result = await BuildService().GetAllByRaceAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Equal("Ana Pérez", result[0].Nombre);
        Assert.Equal("Resuelta", result[1].Estado);
        Assert.Equal(3.3, result[0].Diferencia);
        Assert.Null(result[1].TiempoChip);
    }

    [Fact]
    public async Task List_CarreraNoExiste_LanzaNotFoundException()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Race?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().GetAllByRaceAsync(1));
    }

    [Fact]
    public async Task Summary_CuentaAbiertasYResueltas()
    {
        RaceExists();
        _controversies.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Abierta(1),
                Abierta(2),
                new Controversy { Id = 3, RaceId = 1, Dorsal = "300", Nombre = "Karla", Categoria = "Libre 10K", Estado = "Resuelta" },
            ]);

        var summary = await BuildService().GetSummaryAsync(1);

        Assert.Equal(2, summary.Abiertas);
        Assert.Equal(1, summary.Resueltas);
    }

    [Fact]
    public async Task Resolve_AbiertaAResuelta_PersisteYSeteaResolvedAt()
    {
        RaceExists();
        var disputa = Abierta(7);
        _controversies.Setup(c => c.GetByIdAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(disputa);

        var result = await BuildService().ResolveAsync(1, 7, new ResolveControversyRequest("Resuelta"));

        Assert.Equal("Resuelta", disputa.Estado);
        Assert.NotNull(disputa.ResolvedAt);
        Assert.Equal("Resuelta", result.Estado);
        _controversies.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Resolve_EstadoInvalido_LanzaValidationException()
    {
        RaceExists();
        _controversies.Setup(c => c.GetByIdAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Abierta(7));

        await Assert.ThrowsAsync<ValidationException>(
            () => BuildService().ResolveAsync(1, 7, new ResolveControversyRequest("Revisando")));

        _controversies.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resolve_DisputaNoExiste_LanzaNotFoundException()
    {
        RaceExists();
        _controversies.Setup(c => c.GetByIdAsync(1, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Controversy?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().ResolveAsync(1, 99, new ResolveControversyRequest("Resuelta")));
    }

    [Fact]
    public async Task Resolve_CarreraNoExiste_LanzaNotFoundException()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Race?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().ResolveAsync(1, 7, new ResolveControversyRequest("Resuelta")));
    }
}