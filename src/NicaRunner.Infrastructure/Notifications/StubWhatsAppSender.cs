using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Notifications;

/// <summary>
/// Placeholder hasta integrar WhatsApp Business API (o webhook equivalente).
/// </summary>
public class StubWhatsAppSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public Task<NotificationSendResult> SendAsync(string destino, string mensaje, CancellationToken ct = default) =>
        Task.FromResult(new NotificationSendResult(
            false,
            "Integración de WhatsApp pendiente de configurar (proveedor y credenciales)."));
}
