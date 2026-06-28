namespace NicaRunner.Domain.Entities;

public class Result
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int? RunnerId { get; set; }
    public string? Dorsal { get; set; } // null hasta que se asigna; copiado al momento de asignación
    public DateTime TiempoLlegada { get; set; }
    public int Posicion { get; set; }
    public int? CategoryId { get; set; } // null hasta que el dorsal asignado resuelve la categoría
    public int CapturistaId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Race Race { get; set; } = null!;
    public Runner? Runner { get; set; }
    public RaceCategory? Category { get; set; }
    public User Capturista { get; set; } = null!;
    public ICollection<ResultAudit> AuditEntries { get; set; } = new List<ResultAudit>();
}
