namespace NicaRunner.Domain.Entities;

// Asociación: qué categorías del catálogo global (Category) participan en una carrera.
public class RaceCategory
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int CategoryId { get; set; }

    public Race Race { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
