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

public class AuthServiceChangePasswordTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IGoogleAuthService> _google = new();
    private readonly Mock<IRefreshTokenService> _refresh = new();
    private readonly Mock<IEmailTemplateRenderer> _emailRenderer = new();

    private AuthService BuildService() =>
        new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object,
            _refresh.Object, [], _emailRenderer.Object, Options.Create(new FrontendOptions()),
            new AliasAssigner(_users.Object), Options.Create(new LockoutOptions()),
            NullLogger<AuthService>.Instance);

    [Fact]
    public async Task ChangePassword_CurrentPasswordCorrecta_ActualizaHashYLimpiaFlag()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash-vieja", MustChangePassword = true };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("actual", "hash-vieja")).Returns(true);
        _passwordHasher.Setup(p => p.Hash("nueva-segura")).Returns("hash-nueva");

        await BuildService().ChangePasswordAsync(1, new ChangePasswordRequest("actual", "nueva-segura"));

        Assert.Equal("hash-nueva", user.PasswordHash);
        Assert.False(user.MustChangePassword);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _refresh.Verify(r => r.RevokeAllForUserAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    // login-lockout: con un JWT vigente se puede cambiar la contraseña estando
    // bloqueado para login; el cambio también tiene que levantar el bloqueo.
    [Fact]
    public async Task ChangePassword_CuentaBloqueada_LimpiaElBloqueo()
    {
        var user = new User
        {
            Id = 1,
            Email = "a@b.com",
            PasswordHash = "hash-vieja",
            FailedLoginCount = 5,
            LockedUntilUtc = DateTime.UtcNow.AddMinutes(15),
            LastFailedLoginUtc = DateTime.UtcNow.AddMinutes(-1)
        };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("actual", "hash-vieja")).Returns(true);
        _passwordHasher.Setup(p => p.Hash("nueva-segura")).Returns("hash-nueva");

        await BuildService().ChangePasswordAsync(1, new ChangePasswordRequest("actual", "nueva-segura"));

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedUntilUtc);
        Assert.Null(user.LastFailedLoginUtc);
    }

    [Fact]
    public async Task ChangePassword_CurrentPasswordIncorrecta_LanzaInvalidCredentials()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash-vieja" };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash-vieja")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ChangePasswordAsync(1, new ChangePasswordRequest("mala", "nueva-segura")));

        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _refresh.Verify(r => r.RevokeAllForUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // login-lockout: el bloqueo solo se levanta cuando el cambio prospera. Una
    // contraseña actual equivocada no debe servir para desbloquear la cuenta.
    [Fact]
    public async Task ChangePassword_CurrentPasswordIncorrecta_NoLimpiaElBloqueo()
    {
        var lockedUntil = DateTime.UtcNow.AddMinutes(15);
        var user = new User
        {
            Id = 1,
            Email = "a@b.com",
            PasswordHash = "hash-vieja",
            FailedLoginCount = 5,
            LockedUntilUtc = lockedUntil
        };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash-vieja")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ChangePasswordAsync(1, new ChangePasswordRequest("mala", "nueva-segura")));

        Assert.Equal(5, user.FailedLoginCount);
        Assert.Equal(lockedUntil, user.LockedUntilUtc);
    }

    [Fact]
    public async Task ChangePassword_UsuarioInexistente_LanzaNotFound()
    {
        _users.Setup(u => u.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().ChangePasswordAsync(99, new ChangePasswordRequest("x", "y")));
    }

    [Fact]
    public async Task ChangePassword_UsuarioInactivo_LanzaForbidden()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash-vieja", IsActive = false };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => BuildService().ChangePasswordAsync(1, new ChangePasswordRequest("actual", "nueva-segura")));

        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _refresh.Verify(r => r.RevokeAllForUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
