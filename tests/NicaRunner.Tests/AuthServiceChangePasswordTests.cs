using Moq;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;
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

    private AuthService BuildService() =>
        new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object);

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
    }

    [Fact]
    public async Task ChangePassword_UsuarioInexistente_LanzaNotFound()
    {
        _users.Setup(u => u.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().ChangePasswordAsync(99, new ChangePasswordRequest("x", "y")));
    }
}
