using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

/// <summary>
/// Punto único para enviar una notificación por canal. Encapsula el lookup
/// "sender por canal" que antes duplicaban 3 consumers (AuthService,
/// UserManagementService, NotificationService), y aísla del resto del código
/// la landmine de que <c>INotificationSender</c> como interfaz singular en
/// DI devuelve el ÚLTIMO registrado (StubWhatsApp gana sobre Resend). Los
/// consumers no deberían inyectar <c>INotificationSender</c> ni
/// <c>IEnumerable&lt;INotificationSender&gt;</c> directamente.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Envía por el canal solicitado. Si no hay sender configurado para el
    /// canal, devuelve <c>NotificationSendResult(false, "...")</c> — no lanza.
    /// Los consumers deciden qué hacer con el fallo (log, ignorar, fallar el
    /// endpoint).
    /// </summary>
    Task<NotificationSendResult> SendAsync(
        NotificationChannel channel,
        string destino,
        string mensaje,
        string? subject = null,
        CancellationToken ct = default);
}
