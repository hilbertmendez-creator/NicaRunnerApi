using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Notifications;

public class NotificationDispatcher(IEnumerable<INotificationSender> senders) : INotificationDispatcher
{
    // Construir el mapa una sola vez por scope. senders.ToDictionary tira
    // ArgumentException si dos senders reclaman el mismo Channel — eso es
    // un bug de config que preferimos que rompa temprano en el startup del
    // request antes que enmascarar.
    private readonly Dictionary<NotificationChannel, INotificationSender> _byChannel =
        senders.ToDictionary(s => s.Channel);

    public Task<NotificationSendResult> SendAsync(
        NotificationChannel channel,
        string destino,
        string mensaje,
        string? subject = null,
        CancellationToken ct = default)
    {
        if (!_byChannel.TryGetValue(channel, out var sender))
        {
            return Task.FromResult(new NotificationSendResult(
                Success: false,
                ErrorMessage: $"No hay un proveedor configurado para el canal {channel}."));
        }
        return sender.SendAsync(destino, mensaje, subject, ct);
    }
}
