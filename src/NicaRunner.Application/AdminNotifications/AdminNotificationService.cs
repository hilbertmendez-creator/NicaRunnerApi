using NicaRunner.Application.AdminNotifications.Dtos;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.AdminNotifications;

public class AdminNotificationService(IAdminNotificationRepository repository) : IAdminNotificationService
{
    public async Task NotifyAsync(AdminNotificationType type, string mensaje, int? raceId = null, CancellationToken ct = default)
    {
        repository.Add(new AdminNotification
        {
            Type = type,
            Mensaje = mensaje,
            RaceId = raceId
        });

        await repository.SaveChangesAsync(ct);
    }

    public async Task<AdminNotificationsPageDto> GetRecentAsync(int limit = 20, CancellationToken ct = default)
    {
        var items = await repository.GetRecentAsync(limit, ct);
        var unreadCount = await repository.CountUnreadAsync(ct);

        return new AdminNotificationsPageDto(items.Select(ToDto).ToList(), unreadCount);
    }

    public async Task MarkReadAsync(int id, CancellationToken ct = default)
    {
        var notification = await repository.GetByIdAsync(id, ct);
        if (notification is null || notification.ReadAt is not null)
            return;

        notification.ReadAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);
    }

    public Task MarkAllReadAsync(CancellationToken ct = default) =>
        repository.MarkAllReadAsync(DateTime.UtcNow, ct);

    private static AdminNotificationDto ToDto(AdminNotification n) => new(
        n.Id,
        n.Type.ToString(),
        n.Mensaje,
        n.RaceId,
        n.CreatedAt,
        n.ReadAt is not null);
}
