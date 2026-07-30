using Microsoft.EntityFrameworkCore;
using Moq;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications.EmailTemplates;
using NicaRunner.Application.Users;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Notifications;

namespace NicaRunner.Tests;

public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<INotificationSender> _emailSender = new();
    private readonly IEmailTemplateRenderer _emailRenderer = new EmailTemplateRenderer();
    private readonly FakeAuditLogRepository _auditRepo = new();

    private UserManagementService BuildService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        return new(_users.Object, _passwordHasher.Object, [_emailSender.Object], _emailRenderer,
            new AuditService(_auditRepo), new AliasAssigner(_users.Object));
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
        _emailSender.Setup(s => s.SendAsync("nuevo@b.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
        _emailSender.Verify(s => s.SendAsync("nuevo@b.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);

        // user-alias/user-management: alta genera y persiste el alias (sitio 1, §3.4).
        Assert.Equal("nuevo", created.Username); // "Nuevo" (1 token) -> el token completo
        Assert.Equal("nuevo", dto.Username);
    }

    // user-alias: "Alias collision resolved with numeric suffix" — el primer candidato
    // está tomado, el sondeo prueba el intento 2 (sufijo "2") y ese sí está libre.
    [Fact]
    public async Task CreateAsync_AliasBaseColisiona_AsignaConSufijoNumerico()
    {
        _users.Setup(u => u.EmailExistsAsync("nuevo@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash-temporal");
        _users.Setup(u => u.UsernameExistsAsync("nuevo", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _users.Setup(u => u.UsernameExistsAsync("nuevo2", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        User? created = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => created = u)
            .Returns(Task.CompletedTask);

        var dto = await BuildService().CreateAsync(new CreateUserRequest("nuevo@b.com", "Nuevo", UserRole.Capturista));

        Assert.Equal("nuevo2", created!.Username);
        Assert.Equal("nuevo2", dto.Username);
    }

    // user-alias: "Constraint violation on the final insert" — el sondeo previo no cierra
    // la ventana TOCTOU; el primer SaveChanges choca contra el índice único de Username,
    // el segundo intento (con un alias recién sondeado) tiene éxito.
    [Fact]
    public async Task CreateAsync_ColisionTocTouEnInsert_ReintentaYPersiste()
    {
        _users.Setup(u => u.EmailExistsAsync("nuevo@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash-temporal");

        var saveAttempts = 0;
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                saveAttempts++;
                if (saveAttempts == 1)
                    throw new DbUpdateException("insert falló", new Exception("UNIQUE constraint failed: Users.Username"));
                return Task.CompletedTask;
            });

        var dto = await BuildService().CreateAsync(new CreateUserRequest("nuevo@b.com", "Nuevo", UserRole.Capturista));

        Assert.Equal(2, saveAttempts);
        Assert.NotNull(dto.Username);
    }

    [Fact]
    public async Task CreateAsync_EmailYaExiste_LanzaConflict()
    {
        _users.Setup(u => u.EmailExistsAsync("existe@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => BuildService().CreateAsync(new CreateUserRequest("existe@b.com", "X", UserRole.Lector)));
    }

    // user-alias: "Alias Uniqueness and Collision Resolution" — los 10 intentos de sondeo
    // (base + sufijos 2..10) están todos tomados: no debe propagar un error sin manejar.
    [Fact]
    public async Task CreateAsync_TodosLosCandidatosDeAliasOcupados_LanzaConflict()
    {
        _users.Setup(u => u.EmailExistsAsync("nuevo@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash-temporal");
        _users.Setup(u => u.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => BuildService().CreateAsync(new CreateUserRequest("nuevo@b.com", "Nuevo", UserRole.Capturista)));

        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
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

    // user-management: "Admin edits alias to an available value".
    [Fact]
    public async Task UpdateAsync_CambiaAliasDisponible_LoPersisteYAuditalo()
    {
        var target = new User { Id = 2, Email = "b@b.com", Username = "viejoalias", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _users.Setup(u => u.UsernameExistsAsync("newalias", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(null, null, Username: "newalias"));

        Assert.Equal("newalias", target.Username);
        Assert.Equal("newalias", dto.Username);
        var entry = Assert.Single(_auditRepo.Entries);
        Assert.Equal("Username", entry.Campo);
        Assert.Equal("viejoalias", entry.ValorAnterior);
        Assert.Equal("newalias", entry.ValorNuevo);
    }

    // user-management: "Admin edits alias to a value already taken".
    [Fact]
    public async Task UpdateAsync_CambiaAliasYaTomadoPorOtroUsuario_LanzaConflictYNoAudita()
    {
        var target = new User { Id = 2, Email = "b@b.com", Username = "hmendezv2", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _users.Setup(u => u.UsernameExistsAsync("hmendezv", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(null, null, Username: "hmendezv")));

        Assert.Equal("hmendezv2", target.Username); // sin cambios
        Assert.Empty(_auditRepo.Entries);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AliasConFormatoInvalido_LanzaValidationYNoAudita()
    {
        var target = new User { Id = 2, Email = "b@b.com", Username = "actual", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        await Assert.ThrowsAsync<ValidationException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(null, null, Username: "a@b")));

        Assert.Empty(_auditRepo.Entries);
    }

    [Fact]
    public async Task UpdateAsync_AliasIdenticoAlActual_NoRegistraAuditoriaNiConsultaUnicidad()
    {
        var target = new User { Id = 2, Email = "b@b.com", Username = "mismoalias", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        await BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(null, null, Username: "mismoalias"));

        Assert.Empty(_auditRepo.Entries);
        _users.Verify(u => u.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CambiaNombre_RegistraUnaEntradaDeAuditoriaConAutorYValores()
    {
        var target = new User { Id = 2, Email = "b@b.com", Nombre = "Nombre Viejo", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        await BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(null, null, "  Nombre Nuevo  "));

        Assert.Equal("Nombre Nuevo", target.Nombre); // Trim (E-3)
        var entry = Assert.Single(_auditRepo.Entries);
        Assert.Equal(AuditEntityTypes.User, entry.EntityType);
        Assert.Equal(2, entry.EntityId);
        Assert.Equal("Nombre", entry.Campo);
        Assert.Equal("Nombre Viejo", entry.ValorAnterior);
        Assert.Equal("Nombre Nuevo", entry.ValorNuevo);
        Assert.Equal(1, entry.AutorId);
    }

    [Fact]
    public async Task UpdateAsync_CambiaRolYEstado_RegistraUnaEntradaPorCampo()
    {
        var target = new User { Id = 2, Email = "b@b.com", Nombre = "N", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        await BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(UserRole.Lector, false));

        Assert.Equal(2, _auditRepo.Entries.Count);
        Assert.Contains(_auditRepo.Entries, e => e.Campo == "Role" && e.ValorAnterior == "Capturista" && e.ValorNuevo == "Lector");
        Assert.Contains(_auditRepo.Entries, e => e.Campo == "IsActive" && e.ValorAnterior == "true" && e.ValorNuevo == "false");
    }

    [Fact]
    public async Task UpdateAsync_NombreIdenticoAlActual_NoRegistraAuditoria()
    {
        var target = new User { Id = 2, Email = "b@b.com", Nombre = "Mismo Nombre", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        await BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(null, null, "Mismo Nombre"));

        Assert.Empty(_auditRepo.Entries);
    }

    [Fact]
    public async Task UpdateAsync_NombreVacio_LanzaValidationYNoAudita()
    {
        var target = new User { Id = 2, Email = "b@b.com", Nombre = "N", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        await Assert.ThrowsAsync<ValidationException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(null, null, "   ")));

        Assert.Empty(_auditRepo.Entries);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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

    [Theory]
    [InlineData("hilbert.mendez@gmail.com")]
    [InlineData("evr86.skip@gmail.com")]
    [InlineData("edufisica@ymail.com")]
    public async Task UpdateAsync_UsuarioSemillaIntentaDesactivarse_LanzaForbidden(string email)
    {
        var seed = new User { Id = 2, Email = email, Role = UserRole.Administrador, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(seed);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(null, false)));
    }

    [Theory]
    [InlineData("hilbert.mendez@gmail.com")]
    [InlineData("evr86.skip@gmail.com")]
    [InlineData("edufisica@ymail.com")]
    public async Task UpdateAsync_UsuarioSemillaIntentaCambiarRol_LanzaForbidden(string email)
    {
        var seed = new User { Id = 2, Email = email, Role = UserRole.Administrador, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(seed);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(UserRole.Lector, null)));
    }
}
