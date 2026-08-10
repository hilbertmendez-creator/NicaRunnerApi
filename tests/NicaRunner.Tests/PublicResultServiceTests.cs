using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.PublicResults;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

// design.md Decisión 7: estados vacíos y forma del DTO del enlace público de detalle
// del corredor (GET /api/public/corredor/{shareKey}).
public class PublicResultServiceShareKeyTests
{
    private readonly Mock<IPublicResultTokenRepository> _tokens = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRaceCategoryRepository> _categories = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultRepository> _results = new();

    private PublicResultService BuildService() =>
        new(_tokens.Object, _races.Object, _categories.Object, _runners.Object, _results.Object);

    private static Runner RunnerWithRace(Race race, Category category, string shareKey = "claveXXXXXXXXXXXXXXXX") => new()
    {
        Id = 7, RaceId = race.Id, CategoryId = category.Id, Nombre = "Ana", Apellidos = "Pérez",
        Club = "Club X", Dorsal = "101", PublicShareKey = shareKey, Race = race, Category = category
    };

    [Fact]
    public async Task GetRunnerByShareKeyAsync_ClaveDesconocida_LanzaNotFoundException()
    {
        _runners.Setup(r => r.GetByShareKeyAsync("no-existe", It.IsAny<CancellationToken>())).ReturnsAsync((Runner?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => BuildService().GetRunnerByShareKeyAsync("no-existe"));
    }

    [Fact]
    public async Task GetRunnerByShareKeyAsync_CorredorSinResultado_DevuelveIdentidadYCamposDeTiempoNulos()
    {
        var race = new Race { Id = 1, Nombre = "Carrera Test", FechaCarrera = new DateTime(2026, 6, 1), AdminId = 1 };
        var category = new Category { Id = 2, NombreCategoria = "Juvenil", Distancia = 5, Codigo = "JUV" };
        var runner = RunnerWithRace(race, category);
        _runners.Setup(r => r.GetByShareKeyAsync("claveXXXXXXXXXXXXXXXX", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(race.Id, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Result?)null);

        var dto = await BuildService().GetRunnerByShareKeyAsync("claveXXXXXXXXXXXXXXXX");

        Assert.Equal("Ana", dto.Nombre);
        Assert.Equal("Pérez", dto.Apellidos);
        Assert.Equal("101", dto.Dorsal);
        Assert.Equal("Juvenil", dto.NombreCategoria);
        Assert.Null(dto.TiempoLlegadaUtc);
        Assert.Null(dto.TiempoTranscurridoSegundos);
        Assert.Null(dto.PosicionCategoria);
        Assert.Null(dto.TotalCategoria);
        Assert.Null(dto.PosicionGeneral);
        Assert.Null(dto.TotalGeneral);
        _results.Verify(r => r.GetPlacingCountsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunnerByShareKeyAsync_CarreraSinRaceStartUtc_TiempoLlegadaSinElapsed()
    {
        var race = new Race { Id = 1, Nombre = "Carrera Test", FechaCarrera = new DateTime(2026, 6, 1), RaceStartUtc = null, AdminId = 1 };
        var category = new Category { Id = 2, NombreCategoria = "Juvenil", Distancia = 5, Codigo = "JUV" };
        var runner = RunnerWithRace(race, category);
        var llegada = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var result = new Result { Id = 50, RaceId = race.Id, RunnerId = runner.Id, CategoryId = category.Id, Category = category, TiempoLlegada = llegada, Dorsal = "101" };

        _runners.Setup(r => r.GetByShareKeyAsync("claveXXXXXXXXXXXXXXXX", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(race.Id, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _results.Setup(r => r.GetPlacingCountsAsync(race.Id, category.Id, llegada, result.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlacingCounts(0, 1, 0, 1));

        var dto = await BuildService().GetRunnerByShareKeyAsync("claveXXXXXXXXXXXXXXXX");

        Assert.Equal(llegada, dto.TiempoLlegadaUtc);
        Assert.Null(dto.TiempoTranscurridoSegundos);
        Assert.Equal(1, dto.PosicionCategoria);
        Assert.Equal(1, dto.TotalCategoria);
        Assert.Equal(1, dto.PosicionGeneral);
        Assert.Equal(1, dto.TotalGeneral);
    }

    [Fact]
    public async Task GetRunnerByShareKeyAsync_ConRaceStartUtc_CalculaTiempoTranscurridoYPlacing()
    {
        var start = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        var llegada = start.AddMinutes(42).AddSeconds(17);
        var race = new Race { Id = 1, Nombre = "Carrera Test", FechaCarrera = new DateTime(2026, 6, 1), RaceStartUtc = start, AdminId = 1 };
        var category = new Category { Id = 2, NombreCategoria = "Juvenil", Distancia = 5, Codigo = "JUV" };
        var runner = RunnerWithRace(race, category);
        var result = new Result { Id = 50, RaceId = race.Id, RunnerId = runner.Id, CategoryId = category.Id, Category = category, TiempoLlegada = llegada, Dorsal = "101" };

        _runners.Setup(r => r.GetByShareKeyAsync("claveXXXXXXXXXXXXXXXX", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(race.Id, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _results.Setup(r => r.GetPlacingCountsAsync(race.Id, category.Id, llegada, result.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlacingCounts(2, 5, 3, 10));

        var dto = await BuildService().GetRunnerByShareKeyAsync("claveXXXXXXXXXXXXXXXX");

        Assert.Equal((42 * 60) + 17, dto.TiempoTranscurridoSegundos);
        Assert.Equal(3, dto.PosicionCategoria);
        Assert.Equal(5, dto.TotalCategoria);
        Assert.Equal(4, dto.PosicionGeneral);
        Assert.Equal(10, dto.TotalGeneral);
    }

    [Fact]
    public async Task GetRunnerByShareKeyAsync_ResultadoSinCategoria_UsaCategoriaDelCorredorYPlacingGeneralIgual()
    {
        var race = new Race { Id = 1, Nombre = "Carrera Test", FechaCarrera = new DateTime(2026, 6, 1), RaceStartUtc = null, AdminId = 1 };
        var category = new Category { Id = 2, NombreCategoria = "Juvenil", Distancia = 5, Codigo = "JUV" };
        var runner = RunnerWithRace(race, category);
        var llegada = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var result = new Result { Id = 50, RaceId = race.Id, RunnerId = runner.Id, CategoryId = null, Category = null, TiempoLlegada = llegada, Dorsal = "101" };

        _runners.Setup(r => r.GetByShareKeyAsync("claveXXXXXXXXXXXXXXXX", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(race.Id, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _results.Setup(r => r.GetPlacingCountsAsync(race.Id, category.Id, llegada, result.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlacingCounts(0, 0, 4, 9));

        var dto = await BuildService().GetRunnerByShareKeyAsync("claveXXXXXXXXXXXXXXXX");

        Assert.Equal("Juvenil", dto.NombreCategoria);
        Assert.Equal(5, dto.Distancia);
        Assert.Null(dto.PosicionCategoria);
        Assert.Null(dto.TotalCategoria);
        Assert.Equal(5, dto.PosicionGeneral);
        Assert.Equal(9, dto.TotalGeneral);
    }

    [Fact]
    public async Task GetRunnerByShareKeyAsync_CategoriaDelResultadoDistintaALaDelCorredor_UsaLaDelResultado()
    {
        var race = new Race { Id = 1, Nombre = "Carrera Test", FechaCarrera = new DateTime(2026, 6, 1), RaceStartUtc = null, AdminId = 1 };
        var categoriaCorredor = new Category { Id = 2, NombreCategoria = "Juvenil", Distancia = 5, Codigo = "JUV" };
        var categoriaResultado = new Category { Id = 3, NombreCategoria = "Adultos", Distancia = 10, Codigo = "AD" };
        var runner = RunnerWithRace(race, categoriaCorredor);
        var llegada = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var result = new Result { Id = 50, RaceId = race.Id, RunnerId = runner.Id, CategoryId = categoriaResultado.Id, Category = categoriaResultado, TiempoLlegada = llegada, Dorsal = "101" };

        _runners.Setup(r => r.GetByShareKeyAsync("claveXXXXXXXXXXXXXXXX", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(race.Id, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _results.Setup(r => r.GetPlacingCountsAsync(race.Id, categoriaResultado.Id, llegada, result.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlacingCounts(1, 3, 2, 8));

        var dto = await BuildService().GetRunnerByShareKeyAsync("claveXXXXXXXXXXXXXXXX");

        Assert.Equal("Adultos", dto.NombreCategoria);
        Assert.Equal(10, dto.Distancia);
        Assert.Equal(2, dto.PosicionCategoria);
        Assert.Equal(3, dto.TotalCategoria);
    }

    [Fact]
    public async Task GetRunnerByShareKeyAsync_ConSloganEnLaCarrera_UsaEseSlogan()
    {
        var race = new Race { Id = 1, Nombre = "Carrera Test", FechaCarrera = new DateTime(2026, 6, 1), Slogan = "Corré por tu ciudad", AdminId = 1 };
        var category = new Category { Id = 2, NombreCategoria = "Juvenil", Distancia = 5, Codigo = "JUV" };
        var runner = RunnerWithRace(race, category);
        _runners.Setup(r => r.GetByShareKeyAsync("claveXXXXXXXXXXXXXXXX", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(race.Id, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Result?)null);

        var dto = await BuildService().GetRunnerByShareKeyAsync("claveXXXXXXXXXXXXXXXX");

        Assert.Equal("Corré por tu ciudad", dto.Slogan);
    }

    // Mismo vector dorado que RaceSloganSelectorTests — confirma que el service delega
    // en RaceSloganSelector.Select en vez de reimplementar la cascada/hash acá.
    [Fact]
    public async Task GetRunnerByShareKeyAsync_SinSloganNiDescripcion_UsaLaFraseDeterministaPorHashDelShareKey()
    {
        var race = new Race { Id = 1, Nombre = "Carrera Test", FechaCarrera = new DateTime(2026, 6, 1), AdminId = 1 };
        var category = new Category { Id = 2, NombreCategoria = "Juvenil", Distancia = 5, Codigo = "JUV" };
        var runner = RunnerWithRace(race, category, shareKey: "AAAAAAAAAAAAAAAAAAAAAA");
        _runners.Setup(r => r.GetByShareKeyAsync("AAAAAAAAAAAAAAAAAAAAAA", It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _results.Setup(r => r.GetByRunnerWithCategoryAsync(race.Id, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Result?)null);

        var dto = await BuildService().GetRunnerByShareKeyAsync("AAAAAAAAAAAAAAAAAAAAAA");

        Assert.Equal("Hoy el circuito fue testigo de tu esfuerzo.", dto.Slogan);
    }
}

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

        var runner = new Runner { Id = 7, RaceId = 1, Dorsal = "101", CategoryId = 2, Nombre = "Ana", PublicShareKey = "claveDeAnaXXXXXXXXXXX" };
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
        Assert.Equal("claveDeAnaXXXXXXXXXXX", runnerResult.ShareKey);
    }

    // MODIFIED Requirement "Public Race Results List Response" (spec.md): la lista
    // pública debe traer el ShareKey de cada corredor aunque todavía no tenga una
    // (backfill pendiente) — nunca debe reventar ni omitir la fila.
    [Fact]
    public async Task GetResultsByTokenAsync_CorredorSinShareKeyTodavia_DevuelveShareKeyNulo()
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

        var runnerResult = Assert.Single(Assert.Single(dto.Categorias).Resultados);
        Assert.Null(runnerResult.ShareKey);
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

    [Fact]
    public async Task RevokeTokenAsync_TokenVigente_MarcaIsExpiredYGuarda()
    {
        var token = ValidToken();
        _tokens.Setup(t => t.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        await BuildService().RevokeTokenAsync(raceId: 1, tokenId: 1);

        Assert.True(token.IsExpired);
        _tokens.Verify(t => t.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_TokenInexistente_LanzaNotFoundException()
    {
        _tokens.Setup(t => t.GetByIdAsync(1, 999, It.IsAny<CancellationToken>())).ReturnsAsync((PublicResultToken?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().RevokeTokenAsync(raceId: 1, tokenId: 999));

        _tokens.Verify(t => t.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // El raceId de la ruta no es decorativo: pedir un tokenId que existe pero
    // pertenece a otra carrera tiene que dar 404, no revocar el enlace ajeno.
    [Fact]
    public async Task RevokeTokenAsync_TokenDeOtraCarrera_LanzaNotFoundException()
    {
        _tokens.Setup(t => t.GetByIdAsync(2, 1, It.IsAny<CancellationToken>())).ReturnsAsync((PublicResultToken?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().RevokeTokenAsync(raceId: 2, tokenId: 1));

        _tokens.Verify(t => t.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeTokenAsync_TokenYaRevocado_NoFalla()
    {
        var token = ValidToken();
        token.IsExpired = true;
        _tokens.Setup(t => t.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        await BuildService().RevokeTokenAsync(raceId: 1, tokenId: 1);

        Assert.True(token.IsExpired);
    }

    // Regresión del punto de todo esto: hasta ahora IsExpired se leía pero nadie
    // lo escribía, así que este camino nunca se ejercitaba en producción. Un
    // enlace revocado tiene que morir aunque su FechaExpiracion siga en el futuro.
    [Fact]
    public async Task GetResultsByTokenAsync_TokenRevocadoConFechaFutura_LanzaNotFoundException()
    {
        var revocado = ValidToken();
        revocado.IsExpired = true;
        _tokens.Setup(t => t.GetByTokenAsync("abc123", It.IsAny<CancellationToken>())).ReturnsAsync(revocado);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().GetResultsByTokenAsync("abc123"));

        _races.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllByRaceAsync_TokenRevocado_ExponeRevocadoEnTrue()
    {
        var revocado = ValidToken();
        revocado.IsExpired = true;
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "Carrera Test", AdminId = 1 });
        _tokens.Setup(t => t.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([revocado]);

        var dto = Assert.Single(await BuildService().GetAllByRaceAsync(1));

        Assert.True(dto.Revocado);
    }
}
