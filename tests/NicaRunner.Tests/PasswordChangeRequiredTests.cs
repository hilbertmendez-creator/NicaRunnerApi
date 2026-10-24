using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Api.Auth;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Security;

namespace NicaRunner.Tests;

// Contra la app real: lo que se prueba es por dónde llega el token (cookie del panel o
// header de la app), y eso sólo lo decide el pipeline completo.
public class PasswordChangeRequiredTests
{
    private static readonly JwtSettings TestJwtSettings = new()
    {
        Key = AccountStatusWebApplicationFactory.TestJwtKey,
        Issuer = AccountStatusWebApplicationFactory.TestJwtIssuer,
        Audience = AccountStatusWebApplicationFactory.TestJwtAudience,
        ExpiryMinutes = 60
    };

    private enum Canal { CookieDelPanel, HeaderDeLaApp }

    private static async Task<(AccountStatusWebApplicationFactory Factory, HttpClient Client, int UserId)> BuildClientAsync(
        bool mustChangePassword, string? passwordHash, Canal canal)
    {
        var factory = new AccountStatusWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
        var user = new User
        {
            Email = "admin@x.com",
            Nombre = "Admin",
            Role = UserRole.Administrador,
            MustChangePassword = mustChangePassword,
            PasswordHash = passwordHash,
            Provider = passwordHash is null ? AuthProvider.Google : AuthProvider.Local
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var token = new JwtTokenGenerator(Options.Create(TestJwtSettings)).GenerateToken(user).Token;
        if (canal == Canal.CookieDelPanel)
            client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookieNames.AccessToken}={token}");
        else
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (factory, client, user.Id);
    }

    [Fact]
    public async Task PanelConContraseñaTemporal_RechazaConCodigoPropio()
    {
        var (factory, client, _) = await BuildClientAsync(mustChangePassword: true, passwordHash: "hash", Canal.CookieDelPanel);
        using var _f = factory;

        var response = await client.GetAsync("/api/races");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("password_change_required", await response.Content.ReadAsStringAsync());
    }

    // La pantalla de cambio tiene que poder saber quién es; con la ruta versionada también.
    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/v1/auth/me")]
    public async Task PanelConContraseñaTemporal_DejaPasarLasRutasDeAuth(string ruta)
    {
        var (factory, client, _) = await BuildClientAsync(mustChangePassword: true, passwordHash: "hash", Canal.CookieDelPanel);
        using var _f = factory;

        var response = await client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // La app no tiene pantalla de cambio: exigírselo dejaría al capturista sin capturar.
    [Fact]
    public async Task AppConContraseñaTemporal_NoSeLeExige()
    {
        var (factory, client, _) = await BuildClientAsync(mustChangePassword: true, passwordHash: "hash", Canal.HeaderDeLaApp);
        using var _f = factory;

        var response = await client.GetAsync("/api/races");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Sin contraseña no hay nada que cambiar, y el formulario pediría la actual.
    [Fact]
    public async Task CuentaSoloDeGoogle_NoSeLeExige()
    {
        var (factory, client, _) = await BuildClientAsync(mustChangePassword: true, passwordHash: null, Canal.CookieDelPanel);
        using var _f = factory;

        var response = await client.GetAsync("/api/races");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // El access token sigue siendo el mismo después del cambio: si el "sí" quedara en el
    // cache, el panel rebotaría hasta que venciera el TTL.
    [Fact]
    public async Task DespuesDeCambiarla_LaSiguienteSolicitudPasa()
    {
        var (factory, client, userId) = await BuildClientAsync(mustChangePassword: true, passwordHash: "hash", Canal.CookieDelPanel);
        using var _f = factory;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/races")).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
            var user = await db.Users.FindAsync(userId);
            user!.MustChangePassword = false;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/races")).StatusCode);
    }

    // El interruptor y el fail-open no se prueban contra el host: la factory pasa la
    // configuración por variables de entorno del proceso, y otra clase que la arme en
    // paralelo con otro valor la leería.
    private static DefaultHttpContext PanelContext(int userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "TestAuth"));
        context.Request.Path = "/api/races";
        context.Request.Headers.Cookie = $"{AuthCookieNames.AccessToken}=x";
        return context;
    }

    private static async Task<bool> RunAsync(DefaultHttpContext context, IAccountStatusCache cache, bool enforce)
    {
        var calledNext = false;
        var middleware = new PasswordChangeRequiredMiddleware(
            _ => { calledNext = true; return Task.CompletedTask; },
            NullLogger<PasswordChangeRequiredMiddleware>.Instance);
        await middleware.InvokeAsync(context, cache,
            Options.Create(new AccountStatusOptions { EnforcePasswordChange = enforce }));
        return calledNext;
    }

    [Fact]
    public async Task InterruptorApagado_NiSiquieraConsulta()
    {
        var cache = new Mock<IAccountStatusCache>();

        Assert.True(await RunAsync(PanelContext(1), cache.Object, enforce: false));
        cache.Verify(c => c.IsPasswordChangePendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SiLaConsultaFalla_DejaPasar()
    {
        var cache = new Mock<IAccountStatusCache>();
        cache.Setup(c => c.IsPasswordChangePendingAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("BD caída"));

        Assert.True(await RunAsync(PanelContext(1), cache.Object, enforce: true));
    }
}
