using NicaRunner.Application.Notifications.Dtos;

namespace NicaRunner.Application.Notifications;

public interface INotificationService
{
    Task<List<NotificationDto>> NotifyResultAsync(int resultId, CancellationToken ct = default);
    Task<NotifyAllSummaryDto> NotifyAllAsync(int raceId, CancellationToken ct = default);

    /// <summary>
    /// Barrido de reintentos: envía todo lo que sigue en Pendiente y reintenta
    /// lo que está en Fallida sin haber agotado el máximo de intentos.
    /// Invocado por el endpoint admin protegido, disparado por un cron
    /// externo — mismo patrón que la limpieza de refresh tokens.
    /// </summary>
    Task<NotificationProcessSummaryDto> ProcessPendingAsync(CancellationToken ct = default);

    Task<NotificationDto> GetStatusAsync(int notificationId, CancellationToken ct = default);
}
