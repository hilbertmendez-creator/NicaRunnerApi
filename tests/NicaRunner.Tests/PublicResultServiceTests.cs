using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.PublicResults;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class PublicResultServiceTests
{
    private readonly Mock<IPublicResultTokenRepository> _tokens = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRaceCategoryRepository> _categories = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultRepository> _results = new();

    private PublicResultService BuildService() =>
        new(_tokens.Object, _races.Object, _categories.Object, _runners.Object, _results.Object);

    private PublicResultToken ValidToken(string token = "abc123") => new()
    {
        Id = 1, RaceId = 1, Token = token, FechaExpiracion = DateTime.UtcNow.AddDays(5)
    };

    [Fact]
    public async Task GetResultsByTokenAsync_TokenValido_DevuelveResultadosPorCategoria()
    {
        _tokens.Setup(t => t.GetByTokenAsync("abc123", It.IsAny<CancellationToken>())).ReturnsAsync(ValidToken());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "Carrera Test", AdminId = 1, FechaCarrera = new DateTime(2026, 6, 1) });

        var category = new Category { Id = 2, Codigo = "JUV", NombreCategoria = "Juvenil", Distancia = 5 };
        _categories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([category]);

        var runner = new Runner { Id = 7, RaceId = 1, Dorsal = "101", CategoryId = 2, Nombre = "Ana" };
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([runner]);

        var result = new Result
        {
            Id = 100, RaceId = 1, RunnerId = 7, CategoryId = 2, Dorsal = "101",
            Posicion = 1, TiempoLlegada = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
        };
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([result]);

        var dto = await BuildService().GetResultsByTokenAsync("abc123");

        Assert.Equal("Carrera Test", dto.RaceName);
        var categoria = Assert.Single(dto.Categorias);
        Assert.Equal("Juvenil", categoria.NombreCategoria);
        var runnerResult = Assert.Single(categoria.Resultados);
        Assert.Equal("Ana", runnerResult.Nombre);
        Assert.Equal(1, runnerResult.Posicion);
    }

    [Fact]
    public async Task GetResultsByTokenAsync_TokenExpirado_LanzaNotFoundException()
    {
        var expired = ValidToken();
        expired.FechaExpiracion = DateTime.UtcNow.AddDays(-1);
        _tokens.Setup(t => t.GetByTokenAsync("abc123", It.IsAny<CancellationToken>())).ReturnsAsync(expired);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().GetResultsByTokenAsync("abc123"));

        _races.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetResultsByTokenAsync_TokenInexistente_LanzaNotFoundException()
    {
        _tokens.Setup(t => t.GetByTokenAsync("no-existe", It.IsAny<CancellationToken>())).ReturnsAsync((PublicResultToken?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().GetResultsByTokenAsync("no-existe"));
    }

    [Fact]
    public async Task GetRunnerResultByTokenAsync_CorredorSinResultado_LanzaNotFoundException()
    {
        _tokens.Setup(t => t.GetByTokenAsync("abc123", It.IsAny<CancellationToken>())).ReturnsAsync(ValidToken());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "Carrera Test", AdminId = 1 });
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().GetRunnerResultByTokenAsync("abc123", runnerId: 999));
    }
}
