namespace NicaRunner.Domain.Entities;

public class PublicResultToken
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string Token { get; set; } = string.Empty; // único
    public DateTime FechaExpiracion { get; set; }
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }

    public Race Race { get; set; } = null!;
    public User Creator { get; set; } = null!;
}
