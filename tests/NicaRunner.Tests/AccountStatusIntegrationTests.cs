using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Security;

namespace NicaRunner.Tests;

// backoffice-user-status-toggle PR3, session-revocation spec "Per-request active-status
// check" -- contra la app real, no un mock: solo el pipeline completo confirma el código
// de estado HTTP que JwtBearerHandler escribe tras context.Fail().
public class AccountStatusIntegrationTests
{
    private static readonly JwtSettings TestJwtSettings = new()
    {
        Key = AccountStatusWebApplicationFactory.TestJwtKey,
        Issuer = AccountStatusWebApplicationFactory.TestJwtIssuer,
        Audience = AccountStatusWebApplicationFactory.TestJwtAudience,
        ExpiryMinutes = 60
    };

    private static async Task<(AccountStatusWebApplicationFactory Factory, HttpClient Client)> BuildAuthenticatedClientAsync(bool isActive)
    {
        var factory = new AccountStatusWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
        var user = new User { Email = "juez@x.com", Nombre = "Juez", Role = UserRole.Capturista, IsActive = isActive };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var token = new JwtTokenGenerator(Options.Create(TestJwtSettings)).GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return (factory, client);
    }

    // Tasks.md 5.1 -- carga la garantía de "no hacen falta cambios de cliente" (design.md
    // D1): si esto devolviera 403 en vez de 401, el diseño colapsa.
    [Fact]
    public async Task UsuarioDesactivado_SiguienteSolicitud_Devuelve401NoDevuelve403()
    {
        var (factory, client) = await BuildAuthenticatedClientAsync(isActive: false);
        using var _ = factory;

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // spec.md "Active user unaffected".
    [Fact]
    public async Task UsuarioActivo_NuncaEsRechazadoPorEsteChequeo()
    {
        var (factory, client) = await BuildAuthenticatedClientAsync(isActive: true);
        using var _ = factory;

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
