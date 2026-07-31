using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class AuthServiceLoginTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IGoogleAuthService> _google = new();
    private readonly Mock<IRefreshTokenService> _refresh = new();
    private readonly Mock<IEmailTemplateRenderer> _emailRenderer = new();

    private AuthService BuildService(LockoutOptions? lockout = null) =>
        new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object,
            _refresh.Object, [], _emailRenderer.Object, Options.Create(new FrontendOptions()),
            new AliasAssigner(_users.Object), Options.Create(lockout ?? new LockoutOptions()),
            NullLogger<AuthService>.Instance);

    private void SetupSuccessfulAuth(User user)
    {
        _jwt.Setup(j => j.GenerateToken(user)).Returns(new GeneratedToken("access-token", DateTime.UtcNow.AddHours(1)));
        _refresh.Setup(r => r.IssueAsync(user, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedRefreshToken("refresh-token", DateTime.UtcNow.AddDays(30), Guid.NewGuid()));
    }

    // user-auth: "Login with email" — identificador nuevo, vía GetByEmailOrUsernameAsync.
    [Fact]
    public async Task Login_PorEmail_EmiteAuthResponse()
    {
        var user = new User
        {
            Id = 1, Email = "a@b.com", Nombre = "Ana", Role = UserRole.Administrador,
            PasswordHash = "hash", IsActive = true
        };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correcta", "hash")).Returns(true);
        SetupSuccessfulAuth(user);

        var result = await BuildService().LoginAsync(new LoginRequest("a@b.com", null, "correcta"));

        Assert.Equal("access-token", result.Token);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(1, result.UserId);
        Assert.Equal(UserRole.Administrador, result.Role);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedUntilUtc);
    }

    // user-auth: "Login with alias" — la misma query unificada resuelve por Username.
    [Fact]
    public async Task Login_PorAlias_EmiteAuthResponse()
    {
        var user = new User
        {
            Id = 2, Email = "hmendez@example.com", Username = "hmendezv", Nombre = "Hilbert",
            Role = UserRole.Capturista, PasswordHash = "hash", IsActive = true
        };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("hmendezv", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correcta", "hash")).Returns(true);
        SetupSuccessfulAuth(user);

        var result = await BuildService().LoginAsync(new LoginRequest("hmendezv", null, "correcta"));

        Assert.Equal(2, result.UserId);
        _users.Verify(u => u.GetByEmailOrUsernameAsync("hmendezv", It.IsAny<CancellationToken>()), Times.Once);
    }

    // El identificador se normaliza (trim + minúsculas) antes de la query unificada.
    [Fact]
    public async Task Login_IdentificadorConMayusculasYEspacios_SeNormalizaAntesDeConsultar()
    {
        var user = new User { Id = 1, Email = "hmendez@example.com", PasswordHash = "hash", IsActive = true };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("hmendez@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correcta", "hash")).Returns(true);
        SetupSuccessfulAuth(user);

        await BuildService().LoginAsync(new LoginRequest("  HMendez@Example.com  ", null, "correcta"));

        _users.Verify(u => u.GetByEmailOrUsernameAsync("hmendez@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_PasswordIncorrecta_LanzaInvalidCredentials()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("a@b.com", null, "mala")));
    }

    // user-auth: "Nonexistent identifier" — dummy-hash verify antes de fallar (timing).
    [Fact]
    public async Task Login_UsuarioInexistente_LanzaInvalidCredentialsYVerificaContraHashDummy()
    {
        _users.Setup(u => u.GetByEmailOrUsernameAsync("nadie@b.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("dummy-hash");

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("nadie@b.com", null, "cualquiera")));

        _passwordHasher.Verify(p => p.Verify("cualquiera", "dummy-hash"), Times.Once);
    }

    [Fact]
    public async Task Login_UsuarioInactivo_LanzaInvalidCredentials()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = false };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("a@b.com", null, "correcta")));

        _passwordHasher.Verify(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // Cuenta creada solo por Google login: PasswordHash es null, no puede
    // autenticarse con email/password aunque el email exista.
    [Fact]
    public async Task Login_CuentaSoloGoogle_LanzaInvalidCredentials()
    {
        var user = new User { Id = 1, Email = "g@b.com", PasswordHash = null, IsActive = true, Provider = AuthProvider.Google };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("g@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("g@b.com", null, "cualquiera")));
    }

    // login-lockout: "Wrong password" y "Nonexistent identifier" -> misma excepción/mensaje
    // (user-auth: "Generic Authentication Failure Response").
    [Fact]
    public async Task Login_PasswordIncorrectaYUsuarioInexistente_DevuelvenElMismoMensaje()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash")).Returns(false);
        _users.Setup(u => u.GetByEmailOrUsernameAsync("nadie@b.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("dummy-hash");

        var wrongPasswordEx = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("a@b.com", null, "mala")));
        var nonexistentEx = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("nadie@b.com", null, "cualquiera")));

        Assert.Equal(wrongPasswordEx.Message, nonexistentEx.Message);
    }

    // login-lockout: "Counter increments on failed attempt".
    [Fact]
    public async Task Login_PasswordIncorrecta_IncrementaContadorYRegistraFecha()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true, FailedLoginCount = 2 };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("a@b.com", null, "mala")));

        Assert.Equal(3, user.FailedLoginCount);
        Assert.NotNull(user.LastFailedLoginUtc);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // login-lockout: "Threshold reached locks the account".
    [Fact]
    public async Task Login_QuintoIntentoFallido_BloqueaLaCuentaPor15Minutos()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true, FailedLoginCount = 4 };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash")).Returns(false);

        var before = DateTime.UtcNow;
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("a@b.com", null, "mala")));

        Assert.Equal(5, user.FailedLoginCount);
        Assert.NotNull(user.LockedUntilUtc);
        Assert.InRange(user.LockedUntilUtc!.Value, before.AddMinutes(15), before.AddMinutes(15).AddSeconds(5));
    }

    // login-lockout: "Already-locked account attempts login" — mismo 401 genérico
    // aunque la contraseña sea correcta, y no llama a Verify (mismo patrón que
    // usuario inactivo).
    [Fact]
    public async Task Login_CuentaBloqueada_RechazaConMismaRespuestaAunqueLaPasswordSeaCorrecta()
    {
        var user = new User
        {
            Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true,
            LockedUntilUtc = DateTime.UtcNow.AddMinutes(10)
        };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("a@b.com", null, "correcta")));

        _passwordHasher.Verify(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // El lock expirado (LockedUntilUtc en el pasado) ya no bloquea.
    [Fact]
    public async Task Login_LockExpirado_YaNoBloqueaYPuedeAutenticar()
    {
        var user = new User
        {
            Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true,
            FailedLoginCount = 5, LockedUntilUtc = DateTime.UtcNow.AddMinutes(-1)
        };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correcta", "hash")).Returns(true);
        SetupSuccessfulAuth(user);

        var result = await BuildService().LoginAsync(new LoginRequest("a@b.com", null, "correcta"));

        Assert.Equal(1, result.UserId);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedUntilUtc);
    }

    // login-lockout: "Counter resets on success".
    [Fact]
    public async Task Login_Exitoso_ReseteaContadorYLimpiaLock()
    {
        var user = new User
        {
            Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true, FailedLoginCount = 3
        };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correcta", "hash")).Returns(true);
        SetupSuccessfulAuth(user);

        await BuildService().LoginAsync(new LoginRequest("a@b.com", null, "correcta"));

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedUntilUtc);
    }

    // Threshold = 0 desactiva el lockout sin deploy (design.md §3.3).
    [Fact]
    public async Task Login_ThresholdEnCero_NuncaBloqueaAunqueFallenMuchosIntentos()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true, FailedLoginCount = 99 };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService(new LockoutOptions { Threshold = 0 }).LoginAsync(new LoginRequest("a@b.com", null, "mala")));

        Assert.Equal(100, user.FailedLoginCount);
        Assert.Null(user.LockedUntilUtc);
    }

    // user-auth: "Backward-Compatible Login Payload" — contrato de forma de request:
    // un payload legado que solo trae "email" (sin "identifier") deserializa igual y
    // autentica sin cambios en AuthService ni en el cliente.
    [Fact]
    public async Task Login_PayloadLegadoSoloConEmail_DeserializaYAutenticaIgual()
    {
        const string legacyJson = """{"email":"a@b.com","password":"correcta"}""";
        var request = JsonSerializer.Deserialize<LoginRequest>(legacyJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(request);
        Assert.Null(request!.Identifier);
        Assert.Equal("a@b.com", request.Email);
        Assert.Equal("a@b.com", request.EffectiveIdentifier);

        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true };
        _users.Setup(u => u.GetByEmailOrUsernameAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correcta", "hash")).Returns(true);
        SetupSuccessfulAuth(user);

        var result = await BuildService().LoginAsync(request);

        Assert.Equal(1, result.UserId);
    }
}
