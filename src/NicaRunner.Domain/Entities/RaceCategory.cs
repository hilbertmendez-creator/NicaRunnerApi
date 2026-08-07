namespace NicaRunner.Domain.Entities;

// Asociación: qué categorías del catálogo global (Category) participan en una carrera.
// Además es la UNIDAD DE CRONOMETRAJE: cada categoría tiene su propia salida y su
// propio cierre, con el juez que ejecutó cada acción.
public class RaceCategory
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int CategoryId { get; set; }

    public RaceCategoryStatus Estado { get; set; } = RaceCategoryStatus.Planeada;

    // Cero del cronómetro de esta categoría. Null mientras está Planeada.
    public DateTime? StartUtc { get; set; }
    public int? StartedByUserId { get; set; }

    // ClosedUtc es administrativo: nada se calcula a partir de él.
    public DateTime? ClosedUtc { get; set; }
    public int? ClosedByUserId { get; set; }

    public Race Race { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public User? StartedBy { get; set; }
    public User? ClosedBy { get; set; }
}
