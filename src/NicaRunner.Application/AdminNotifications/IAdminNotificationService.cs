using NicaRunner.Application.AdminNotifications.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.AdminNotifications;

public interface IAdminNotificationService
{
    Task NotifyAsync(AdminNotificationType type, string mensaje, int? raceId = null, CancellationToken ct = default);
    Task<AdminNotificationsPageDto> GetRecentAsync(int limit = 20, CancellationToken ct = default);
    Task MarkReadAsync(int id, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
}
