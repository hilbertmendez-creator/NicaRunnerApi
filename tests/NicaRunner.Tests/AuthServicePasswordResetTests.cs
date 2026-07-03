using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class AuthServicePasswordResetTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IGoogleAuthService> _google = new();
    private readonly Mock<INotificationSender> _emailSender = new();
    private readonly IOptions<FrontendOptions> _frontendOptions = Options.Create(new FrontendOptions { BaseUrl = "https://backoffice.nicarunner.test" });

    private AuthService BuildService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        return new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object, [_emailSender.Object], _frontendOptions);
    }

    [Fact]
    public async Task ForgotPassword_EmailExistenteLocal_GeneraTokenYEnviaEmail()
    {
        var user = new User { Id = 1, Email = "a@b.com", Provider = AuthProvider.Local, PasswordHash = "hash" };
        _users.Setup(u => u.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _emailSender.Setup(s => s.SendAsync("a@b.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        await BuildService().ForgotPasswordAsync(new ForgotPasswordRequest("a@b.com"));

        Assert.NotNull(user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetTokenExpiry);
        _emailSender.Verify(s => s.SendAsync("a@b.com", It.Is<string>(m => m.Contains("https://backoffice.nicarunner.test")), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_EmailInexistente_NoLanzaYNoEnviaEmail()
    {
        _users.Setup(u => u.GetByEmailAsync("nadie@b.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await BuildService().ForgotPasswordAsync(new ForgotPasswordRequest("nadie@b.com"));

        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_UsuarioSoloGoogle_NoEnviaEmail()
    {
        var user = new User { Id = 2, Email = "g@b.com", Provider = AuthProvider.Google, PasswordHash = null };
        _users.Setup(u => u.GetByEmailAsync("g@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await BuildService().ForgotPasswordAsync(new ForgotPasswordRequest("g@b.com"));

        Assert.Null(user.PasswordResetToken);
        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_TokenValido_ActualizaPasswordYLimpiaToken()
    {
        var user = new User
        {
            Id = 1,
            Email = "a@b.com",
            PasswordHash = "hash-vieja",
            PasswordResetToken = "token-123",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(10),
            MustChangePassword = true
        };
        _users.Setup(u => u.GetByResetTokenAsync("token-123", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Hash("nueva-segura")).Returns("hash-nueva");

        await BuildService().ResetPasswordAsync(new ResetPasswordRequest("token-123", "nueva-segura"));

        Assert.Equal("hash-nueva", user.PasswordHash);
        Assert.Null(user.PasswordResetToken);
        Assert.Null(user.PasswordResetTokenExpiry);
        Assert.False(user.MustChangePassword);
    }

    [Fact]
    public async Task ResetPassword_TokenExpirado_LanzaInvalidCredentials()
    {
        var user = new User
        {
            Id = 1,
            Email = "a@b.com",
            PasswordResetToken = "token-123",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-1)
        };
        _users.Setup(u => u.GetByResetTokenAsync("token-123", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ResetPasswordAsync(new ResetPasswordRequest("token-123", "nueva-segura")));
    }

    [Fact]
    public async Task ResetPassword_TokenInexistente_LanzaInvalidCredentials()
    {
        _users.Setup(u => u.GetByResetTokenAsync("no-existe", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ResetPasswordAsync(new ResetPasswordRequest("no-existe", "nueva-segura")));
    }

    [Fact]
    public async Task ForgotPassword_SinSenderDeEmailRegistrado_NoLanzaExcepcion()
    {
        var user = new User { Id = 1, Email = "a@b.com", Provider = AuthProvider.Local, PasswordHash = "hash" };
        _users.Setup(u => u.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = new AuthService(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object, [], _frontendOptions);

        await service.ForgotPasswordAsync(new ForgotPasswordRequest("a@b.com"));

        Assert.NotNull(user.PasswordResetToken);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
