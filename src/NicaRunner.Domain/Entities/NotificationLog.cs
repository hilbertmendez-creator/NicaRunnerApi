namespace NicaRunner.Domain.Entities;

public enum NotificationChannel
{
    Email,
    WhatsApp
}

public enum NotificationStatus
{
    Pendiente,
    Enviada,
    Fallida
}

public class NotificationLog
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int RunnerId { get; set; }
    public int ResultId { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pendiente;
    public string Mensaje { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    public Race Race { get; set; } = null!;
    public Runner Runner { get; set; } = null!;
    public Result Result { get; set; } = null!;
}
