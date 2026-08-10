namespace NicaRunner.Domain.Entities;

public enum AdminNotificationType
{
    DorsalDuplicado,
    UsuarioCreado,
    ImportacionConErrores
}

/// <summary>
/// Feed de eventos administrativos para la campana del topbar. Append-only.
/// Distinto de NotificationLog (que registra avisos a CORREDORES por email/WhatsApp).
/// </summary>
public class AdminNotification
{
    public int Id { get; set; }
    public AdminNotificationType Type { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? RaceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
