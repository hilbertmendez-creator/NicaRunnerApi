using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public record NotificationSendResult(bool Success, string? ErrorMessage);

/// <summary>
/// Punto de extensión por canal. Cuando existan credenciales reales (SendGrid, WhatsApp
/// Business API), se agrega una implementación concreta en Infrastructure y se reemplaza
/// el registro del stub correspondiente en Program.cs — Application y Api no cambian.
/// </summary>
public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task<NotificationSendResult> SendAsync(string destino, string mensaje, string? subject = null, CancellationToken ct = default);
}
