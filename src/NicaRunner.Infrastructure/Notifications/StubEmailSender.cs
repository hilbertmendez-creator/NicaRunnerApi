using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Notifications;

/// <summary>
/// Placeholder hasta integrar un proveedor real (ej. SendGrid). Registra el intento
/// como fallido de forma honesta en vez de simular un envío que no ocurrió.
/// </summary>
public class StubEmailSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public Task<NotificationSendResult> SendAsync(string destino, string mensaje, CancellationToken ct = default) =>
        Task.FromResult(new NotificationSendResult(
            false,
            "Integración de email pendiente de configurar (proveedor y credenciales)."));
}
