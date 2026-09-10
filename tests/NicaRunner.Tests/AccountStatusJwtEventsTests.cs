using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Api.Auth;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Infrastructure.Security;

namespace NicaRunner.Tests;

// backoffice-user-status-toggle PR3, design.md D4 -- matriz de fail-open (el caso
// 401-vs-403 lo cubre AccountStatusIntegrationTests contra el host real).
public class AccountStatusJwtEventsTests
{
    private readonly Mock<IAccountStatusCache> _cache = new();
    private readonly TestLogger _logger = new();

    private static TokenValidatedContext BuildContext(int? userId, IServiceProvider services)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler));
        var context = new TokenValidatedContext(httpContext, scheme, new JwtBearerOptions());
        var claims = userId is { } id ? new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) } : [];
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        return context;
    }

    private IServiceProvider BuildServices(bool enforcePerRequest)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_cache.Object);
        services.AddSingleton<ILogger<AccountStatusJwtEvents>>(_logger);
        services.AddSingleton<IOptions<AccountStatusOptions>>(
            Options.Create(new AccountStatusOptions { EnforcePerRequest = enforcePerRequest }));
        return services.BuildServiceProvider();
    }

    // Tasks.md 5.4: flag apagado -> ni cache ni BD se tocan.
    [Fact]
    public async Task FlagApagado_NoConsultaElCacheNiLaBd()
    {
        var context = BuildContext(1, BuildServices(enforcePerRequest: false));
        await AccountStatusJwtEvents.OnTokenValidated(context);
        _cache.Verify(c => c.IsActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(context.Result);
    }

    // D4 fila 4: IsActive == false -> Fail() + LogInformation.
    [Fact]
    public async Task UsuarioInactivo_RechazaLaSolicitudYRegistraLogInformation()
    {
        _cache.Setup(c => c.IsActiveAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var context = BuildContext(2, BuildServices(enforcePerRequest: true));
        await AccountStatusJwtEvents.OnTokenValidated(context);
        Assert.False(context.Result?.Succeeded);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information);
    }

    // D4 filas 1 y 2: infra caída o usuario borrado (NotFoundException) -> mismo catch.
    public static IEnumerable<object[]> FailOpenExceptions =>
    [
        [new InvalidOperationException("DB caída")],
        [new NotFoundException("No existe el usuario con id 3.")]
    ];

    [Theory]
    [MemberData(nameof(FailOpenExceptions))]
    public async Task CacheLanzaOUsuarioNoEncontrado_PermiteYRegistraLogWarning(Exception thrown)
    {
        _cache.Setup(c => c.IsActiveAsync(3, It.IsAny<CancellationToken>())).ThrowsAsync(thrown);
        var context = BuildContext(3, BuildServices(enforcePerRequest: true));
        await AccountStatusJwtEvents.OnTokenValidated(context);
        Assert.Null(context.Result);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    // D4 fila 3: claim NameIdentifier ausente -> permite + LogWarning (fail-open).
    [Fact]
    public async Task ClaimNameIdentifierAusente_PermiteYRegistraLogWarning()
    {
        var context = BuildContext(null, BuildServices(enforcePerRequest: true));
        await AccountStatusJwtEvents.OnTokenValidated(context);
        Assert.Null(context.Result);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
        _cache.Verify(c => c.IsActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class TestLogger : ILogger<AccountStatusJwtEvents>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
