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

    private AuthService BuildService() =>
        new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object,
            _refresh.Object, [], _emailRenderer.Object, Options.Create(new FrontendOptions()));

    [Fact]
    public async Task Login_CredencialesCorrectas_EmiteAuthResponse()
    {
        var user = new User
        {
            Id = 1, Email = "a@b.com", Nombre = "Ana", Role = UserRole.Administrador,
            PasswordHash = "hash", IsActive = true
        };
        _users.Setup(u => u.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correcta", "hash")).Returns(true);
        _jwt.Setup(j => j.GenerateToken(user)).Returns(new GeneratedToken("access-token", DateTime.UtcNow.AddHours(1)));
        _refresh.Setup(r => r.IssueAsync(user, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedRefreshToken("refresh-token", DateTime.UtcNow.AddDays(30), Guid.NewGuid()));

        var result = await BuildService().LoginAsync(new LoginRequest("a@b.com", "correcta"));

        Assert.Equal("access-token", result.Token);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(1, result.UserId);
        Assert.Equal(UserRole.Administrador, result.Role);
    }

    [Fact]
    public async Task Login_PasswordIncorrecta_LanzaInvalidCredentials()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = true };
        _users.Setup(u => u.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("a@b.com", "mala")));
    }

    [Fact]
    public async Task Login_UsuarioInexistente_LanzaInvalidCredentials()
    {
        _users.Setup(u => u.GetByEmailAsync("nadie@b.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("nadie@b.com", "cualquiera")));
    }

    [Fact]
    public async Task Login_UsuarioInactivo_LanzaInvalidCredentials()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", IsActive = false };
        _users.Setup(u => u.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("a@b.com", "correcta")));

        _passwordHasher.Verify(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // Cuenta creada solo por Google login: PasswordHash es null, no puede
    // autenticarse con email/password aunque el email exista.
    [Fact]
    public async Task Login_CuentaSoloGoogle_LanzaInvalidCredentials()
    {
        var user = new User { Id = 1, Email = "g@b.com", PasswordHash = null, IsActive = true, Provider = AuthProvider.Google };
        _users.Setup(u => u.GetByEmailAsync("g@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().LoginAsync(new LoginRequest("g@b.com", "cualquiera")));
    }
}
