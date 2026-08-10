namespace NicaRunner.Domain.Entities;

public class PublicResultToken
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string Token { get; set; } = string.Empty; // único
    public DateTime FechaExpiracion { get; set; }

    /// <summary>
    /// Revocación manual: mata el enlace al instante, sin importar
    /// FechaExpiracion. Lo setea el administrador desde el back office cuando
    /// un enlace se filtró y hay que cortarlo antes de su vencimiento natural.
    /// ResolveValidTokenAsync lo evalúa antes que la fecha.
    /// </summary>
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }

    public Race Race { get; set; } = null!;
    public User Creator { get; set; } = null!;
}
