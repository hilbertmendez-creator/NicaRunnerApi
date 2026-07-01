using Moq;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class AuthServiceGoogleLoginTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IGoogleAuthService> _google = new();

    private AuthService BuildService() =>
        new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object);

    private void SetupTokenGenerator() =>
        _jwt.Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns(new GeneratedToken("fake-jwt", DateTime.UtcNow.AddHours(1)));

    // AC-4: token invalido/expirado -> InvalidCredentialsException, sin tocar el repositorio.
    [Fact]
    public async Task GoogleLogin_TokenInvalido_LanzaInvalidCredentials()
    {
        _google.Setup(g => g.ValidateIdTokenAsync("token-malo", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleUserInfo?)null);

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.GoogleLoginAsync(new GoogleLoginRequest("token-malo")));

        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC-3: email nuevo -> crea usuario Provider.Google sin PasswordHash.
    [Fact]
    public async Task GoogleLogin_EmailNuevo_CreaUsuarioGoogle()
    {
        _google.Setup(g => g.ValidateIdTokenAsync("token-ok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo("sub-1", "nuevo@gmail.com", "Nuevo Usuario"));
        _users.Setup(u => u.GetByGoogleIdAsync("sub-1", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _users.Setup(u => u.GetByEmailAsync("nuevo@gmail.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        SetupTokenGenerator();

        User? created = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => created = u)
            .Returns(Task.CompletedTask);

        var result = await BuildService().GoogleLoginAsync(new GoogleLoginRequest("token-ok"));

        Assert.Equal("nuevo@gmail.com", result.Email);
        Assert.NotNull(created);
        Assert.Equal(AuthProvider.Google, created!.Provider);
        Assert.Null(created.PasswordHash);
        Assert.Equal("sub-1", created.GoogleId);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.MustChangePassword);
    }

    // AC-2: email existente con password local -> vincula GoogleId, no duplica usuario.
    [Fact]
    public async Task GoogleLogin_EmailExistenteConPassword_VinculaCuenta()
    {
        var existing = new User
        {
            Id = 5,
            Email = "existente@gmail.com",
            PasswordHash = "hash-local",
            Nombre = "Existente",
            Role = UserRole.Capturista
        };
        _google.Setup(g => g.ValidateIdTokenAsync("token-ok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo("sub-2", "existente@gmail.com", "Existente"));
        _users.Setup(u => u.GetByGoogleIdAsync("sub-2", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _users.Setup(u => u.GetByEmailAsync("existente@gmail.com", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupTokenGenerator();

        var result = await BuildService().GoogleLoginAsync(new GoogleLoginRequest("token-ok"));

        Assert.Equal(5, result.UserId);
        Assert.Equal("sub-2", existing.GoogleId);
        Assert.Equal(AuthProvider.LocalAndGoogle, existing.Provider);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC-1: usuario ya vinculado por GoogleId -> sesion directa, sin duplicar.
    [Fact]
    public async Task GoogleLogin_UsuarioYaVinculado_DevuelveSesion()
    {
        var linked = new User
        {
            Id = 7,
            Email = "vinculado@gmail.com",
            Nombre = "Vinculado",
            GoogleId = "sub-3",
            Provider = AuthProvider.Google
        };
        _google.Setup(g => g.ValidateIdTokenAsync("token-ok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo("sub-3", "vinculado@gmail.com", "Vinculado"));
        _users.Setup(u => u.GetByGoogleIdAsync("sub-3", It.IsAny<CancellationToken>())).ReturnsAsync(linked);
        SetupTokenGenerator();

        var result = await BuildService().GoogleLoginAsync(new GoogleLoginRequest("token-ok"));

        Assert.Equal(7, result.UserId);
        _users.Verify(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Edge case: usuario inactivo -> ForbiddenException, no emite token.
    [Fact]
    public async Task GoogleLogin_UsuarioInactivo_LanzaForbidden()
    {
        var inactive = new User
        {
            Id = 9,
            Email = "inactivo@gmail.com",
            Nombre = "Inactivo",
            GoogleId = "sub-4",
            Provider = AuthProvider.Google,
            IsActive = false
        };
        _google.Setup(g => g.ValidateIdTokenAsync("token-ok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo("sub-4", "inactivo@gmail.com", "Inactivo"));
        _users.Setup(u => u.GetByGoogleIdAsync("sub-4", It.IsAny<CancellationToken>())).ReturnsAsync(inactive);

        var service = BuildService();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GoogleLoginAsync(new GoogleLoginRequest("token-ok")));

        _jwt.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
