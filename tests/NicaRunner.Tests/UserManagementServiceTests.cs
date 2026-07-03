using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Users;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<INotificationSender> _emailSender = new();

    private UserManagementService BuildService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        return new(_users.Object, _passwordHasher.Object, [_emailSender.Object]);
    }

    [Fact]
    public async Task GetAllAsync_DevuelveTodosMapeadosADto()
    {
        _users.Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new User { Id = 1, Email = "a@b.com", Nombre = "A", Role = UserRole.Administrador, IsActive = true },
            new User { Id = 2, Email = "c@d.com", Nombre = "C", Role = UserRole.Lector, IsActive = false },
        ]);

        var result = await BuildService().GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("a@b.com", result[0].Email);
        Assert.False(result[1].IsActive);
    }

    [Fact]
    public async Task CreateAsync_EmailNuevo_CreaUsuarioConPasswordTemporalYEnviaEmail()
    {
        _users.Setup(u => u.EmailExistsAsync("nuevo@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash-temporal");
        _emailSender.Setup(s => s.SendAsync("nuevo@b.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        User? created = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => created = u)
            .Returns(Task.CompletedTask);

        var dto = await BuildService().CreateAsync(new CreateUserRequest("nuevo@b.com", "Nuevo", UserRole.Capturista));

        Assert.NotNull(created);
        Assert.Equal("hash-temporal", created!.PasswordHash);
        Assert.True(created.MustChangePassword);
        Assert.Equal(AuthProvider.Local, created.Provider);
        Assert.Equal("nuevo@b.com", dto.Email);
        _emailSender.Verify(s => s.SendAsync("nuevo@b.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmailYaExiste_LanzaConflict()
    {
        _users.Setup(u => u.EmailExistsAsync("existe@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => BuildService().CreateAsync(new CreateUserRequest("existe@b.com", "X", UserRole.Lector)));
    }

    [Fact]
    public async Task UpdateAsync_CambiaRolYEstadoDeOtroUsuario()
    {
        var target = new User { Id = 2, Email = "b@b.com", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        var dto = await BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(UserRole.Lector, false));

        Assert.Equal(UserRole.Lector, target.Role);
        Assert.False(target.IsActive);
        Assert.Equal(UserRole.Lector, dto.Role);
    }

    [Fact]
    public async Task UpdateAsync_AdminIntentaDesactivarseASiMismo_LanzaForbidden()
    {
        var self = new User { Id = 1, Email = "a@b.com", Role = UserRole.Administrador, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(self);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 1, new UpdateUserRequest(null, false)));
    }

    [Fact]
    public async Task UpdateAsync_AdminIntentaCambiarSuPropioRol_LanzaForbidden()
    {
        var self = new User { Id = 1, Email = "a@b.com", Role = UserRole.Administrador, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(self);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 1, new UpdateUserRequest(UserRole.Lector, null)));
    }

    [Fact]
    public async Task UpdateAsync_UsuarioInexistente_LanzaNotFound()
    {
        _users.Setup(u => u.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 99, new UpdateUserRequest(UserRole.Lector, null)));
    }
}
