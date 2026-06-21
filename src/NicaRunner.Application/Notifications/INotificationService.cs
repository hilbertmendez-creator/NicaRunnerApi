using NicaRunner.Application.Notifications.Dtos;

namespace NicaRunner.Application.Notifications;

public interface INotificationService
{
    Task<List<NotificationDto>> NotifyResultAsync(int resultId, CancellationToken ct = default);
    Task<NotifyAllSummaryDto> NotifyAllAsync(int raceId, CancellationToken ct = default);
    Task<NotificationDto> GetStatusAsync(int notificationId, CancellationToken ct = default);
}
