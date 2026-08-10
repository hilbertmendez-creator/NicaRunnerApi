using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class AdminNotificationServiceTests
{
    private readonly Mock<IAdminNotificationRepository> _repository = new();

    private AdminNotificationService BuildService() => new(_repository.Object);

    [Fact]
    public async Task NotifyAsync_AgregaYPersisteLaNotificacion()
    {
        AdminNotification? added = null;
        _repository.Setup(r => r.Add(It.IsAny<AdminNotification>()))
            .Callback<AdminNotification>(n => added = n);

        await BuildService().NotifyAsync(AdminNotificationType.DorsalDuplicado, "Dorsal '105' duplicado en la carrera #1.", raceId: 1);

        Assert.NotNull(added);
        Assert.Equal(AdminNotificationType.DorsalDuplicado, added!.Type);
        Assert.Equal("Dorsal '105' duplicado en la carrera #1.", added.Mensaje);
        Assert.Equal(1, added.RaceId);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecentAsync_DevuelveItemsYUnreadCountTotal_NoSoloLoDeLaPagina()
    {
        // 5 notificaciones sin leer en total, pero GetRecentAsync con limit=2 solo
        // devuelve una página de 2 — UnreadCount debe reflejar las 5, no las 2 devueltas.
        var items = new List<AdminNotification>
        {
            new() { Id = 1, Type = AdminNotificationType.UsuarioCreado, Mensaje = "Uno", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Type = AdminNotificationType.ImportacionConErrores, Mensaje = "Dos", CreatedAt = DateTime.UtcNow, ReadAt = DateTime.UtcNow }
        };
        _repository.Setup(r => r.GetRecentAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(items);
        _repository.Setup(r => r.CountUnreadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);

        var page = await BuildService().GetRecentAsync(2);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(5, page.UnreadCount);
        Assert.False(page.Items[0].Leida);
        Assert.True(page.Items[1].Leida);
        Assert.Equal("UsuarioCreado", page.Items[0].Type);
    }

    [Fact]
    public async Task MarkReadAsync_IdDesconocido_NoOpSinLanzar()
    {
        _repository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((AdminNotification?)null);

        await BuildService().MarkReadAsync(999);

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkReadAsync_NotificacionExistente_MarcaReadAtYPersiste()
    {
        var notification = new AdminNotification { Id = 1, Type = AdminNotificationType.UsuarioCreado, Mensaje = "X" };
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        await BuildService().MarkReadAsync(1);

        Assert.NotNull(notification.ReadAt);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAllReadAsync_DelegaAlRepositorio()
    {
        await BuildService().MarkAllReadAsync();

        _repository.Verify(r => r.MarkAllReadAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
