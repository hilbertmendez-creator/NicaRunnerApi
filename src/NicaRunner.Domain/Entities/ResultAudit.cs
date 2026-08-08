namespace NicaRunner.Domain.Entities;

public class ResultAudit
{
    public int Id { get; set; }
    public int ResultId { get; set; }

    // Antes "AdminId": ahora un juez también anula sus propias capturas y genera
    // auditoría, no solo un Admin editando. El nombre viejo mentía sobre quién actúa.
    public int ActorUserId { get; set; }
    public string CampoModificado { get; set; } = string.Empty; // "TiempoLlegada", "Dorsal", "Posicion"
    public string ValorAnterior { get; set; } = string.Empty;
    public string ValorNuevo { get; set; } = string.Empty;
    public string? Razon { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Result Result { get; set; } = null!;
    public User Admin { get; set; } = null!;
}
