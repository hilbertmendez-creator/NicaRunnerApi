using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Security;

namespace NicaRunner.Tests;

// race-judge-guard -- contra la app real: lo que se prueba es que el filtro esté puesto
// en cada endpoint que un Capturista alcanza, y eso sólo lo confirma el pipeline completo.
public sealed class RaceJudgeGuardIntegrationTests : IAsyncLifetime
{
    private const string Codigo = "ABC234";

    private static readonly JwtSettings TestJwtSettings = new()
    {
        Key = AccountStatusWebApplicationFactory.TestJwtKey,
        Issuer = AccountStatusWebApplicationFactory.TestJwtIssuer,
        Audience = AccountStatusWebApplicationFactory.TestJwtAudience,
        ExpiryMinutes = 60
    };

    private readonly AccountStatusWebApplicationFactory _factory = new();
    private User _admin = null!;
    private User _juez = null!;
    private User _ajeno = null!;
    private User _lector = null!;
    private int _raceId;
    private int _runnerId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
        _admin = new User { Email = "admin@x.com", Nombre = "Admin", Role = UserRole.Administrador };
        _juez = new User { Email = "juez@x.com", Nombre = "Juez", Role = UserRole.Capturista };
        _ajeno = new User { Email = "ajeno@x.com", Nombre = "Ajeno", Role = UserRole.Capturista };
        _lector = new User { Email = "lector@x.com", Nombre = "Lector", Role = UserRole.Lector };
        db.Users.AddRange(_admin, _juez, _ajeno, _lector);
        await db.SaveChangesAsync();

        var race = new Race { Nombre = "5K Managua", FechaCarrera = DateTime.UtcNow.Date, JoinCode = Codigo, AdminId = _admin.Id };
        db.Races.Add(race);
        await db.SaveChangesAsync();
        db.RaceJudges.Add(new RaceJudge { RaceId = race.Id, UserId = _juez.Id });
        var categoria = new Category { Codigo = "LIB", NombreCategoria = "Libre", Distancia = 5 };
        db.Categories.Add(categoria);
        await db.SaveChangesAsync();
        var runner = new Runner
        {
            RaceId = race.Id, Nombre = "Ana", Dorsal = "101", Telefono = "88888888",
            Email = "ana@x.com", CategoryId = categoria.Id, PublicShareKey = "k1"
        };
        db.Runners.Add(runner);
        await db.SaveChangesAsync();

        _raceId = race.Id;
        _runnerId = runner.Id;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient ClientFor(User user)
    {
        var client = _factory.CreateClient();
        var token = new JwtTokenGenerator(Options.Create(TestJwtSettings)).GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }

    // Cada endpoint que [Authorize(Roles)] le abre a un Capturista y lleva raceId.
    private IEnumerable<Func<HttpClient, Task<HttpResponseMessage>>> EndpointsDeCapturista() =>
    [
        c => c.GetAsync($"/api/races/{_raceId}/runners"),
        c => c.GetAsync($"/api/races/{_raceId}/runners/{_runnerId}"),
        c => c.PutAsJsonAsync($"/api/races/{_raceId}/runners/{_runnerId}", new { nombre = "Ana", dorsal = "101" }),
        c => c.GetAsync($"/api/races/{_raceId}/results"),
        c => c.PostAsJsonAsync($"/api/races/{_raceId}/results", new { }),
        c => c.GetAsync($"/api/races/{_raceId}/categories"),
        c => c.PostAsJsonAsync($"/api/races/{_raceId}/categories/start", new { categoryIds = new[] { 1 } }),
        c => c.PostAsJsonAsync($"/api/races/{_raceId}/categories/close", new { categoryIds = new[] { 1 } }),
        c => c.PostAsync($"/api/races/{_raceId}/start", null),
        c => c.PostAsync($"/api/races/{_raceId}/close", null),
        c => c.GetAsync($"/api/v1/races/{_raceId}/runners"),
    ];

    [Fact]
    public async Task CapturistaQueNoEsJuez_RecibeForbiddenEnTodaRutaDeLaCarrera()
    {
        var client = ClientFor(_ajeno);

        foreach (var llamar in EndpointsDeCapturista())
        {
            var response = await llamar(client);
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden,
                $"{response.RequestMessage!.Method} {response.RequestMessage.RequestUri} devolvió {(int)response.StatusCode}");
        }
    }

    [Fact]
    public async Task CapturistaJuez_NoEsFrenadoPorLaGuarda()
    {
        var client = ClientFor(_juez);

        foreach (var llamar in EndpointsDeCapturista())
        {
            // Puede fallar por reglas de negocio (carrera sin arrancar, cuerpo inválido):
            // lo único que se prueba es que la guarda no lo frena.
            var response = await llamar(client);
            Assert.True(response.StatusCode != HttpStatusCode.Forbidden,
                $"{response.RequestMessage!.Method} {response.RequestMessage.RequestUri} devolvió 403");
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/races/{_raceId}/runners")).StatusCode);
    }

    [Fact]
    public async Task CapturistaAjeno_SeUneConElCodigo_YPasaAOperarLaCarrera()
    {
        var client = ClientFor(_ajeno);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/races/{_raceId}/runners")).StatusCode);

        var join = await client.PostAsJsonAsync("/api/races/join", new { code = Codigo });
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/races/{_raceId}/runners")).StatusCode);
    }

    [Fact]
    public async Task AdministradorYLector_NoPasanPorLaGuarda()
    {
        Assert.Equal(HttpStatusCode.OK, (await ClientFor(_admin).GetAsync($"/api/races/{_raceId}/runners")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ClientFor(_lector).GetAsync($"/api/races/{_raceId}/runners")).StatusCode);
    }

    [Fact]
    public async Task ElCodigoDeUnion_SoloLoVenElAdministradorYLosJueces()
    {
        Assert.Equal(Codigo, await CodigoEnListado(ClientFor(_admin)));
        Assert.Equal(Codigo, await CodigoEnListado(ClientFor(_juez)));
        Assert.Equal(string.Empty, await CodigoEnListado(ClientFor(_ajeno)));
        Assert.Equal(string.Empty, await CodigoEnListado(ClientFor(_lector)));

        Assert.Equal(Codigo, await CodigoEnDetalle(ClientFor(_juez)));
        Assert.Equal(string.Empty, await CodigoEnDetalle(ClientFor(_ajeno)));
    }

    private async Task<string> CodigoEnListado(HttpClient client)
    {
        using var json = JsonDocument.Parse(await client.GetStringAsync("/api/races"));
        return json.RootElement.GetProperty("items").EnumerateArray()
            .Single(r => r.GetProperty("id").GetInt32() == _raceId)
            .GetProperty("joinCode").GetString()!;
    }

    private async Task<string> CodigoEnDetalle(HttpClient client)
    {
        using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/races/{_raceId}"));
        return json.RootElement.GetProperty("joinCode").GetString()!;
    }
}
