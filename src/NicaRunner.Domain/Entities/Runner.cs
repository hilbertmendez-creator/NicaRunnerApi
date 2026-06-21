namespace NicaRunner.Domain.Entities;

public class Runner
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Dorsal { get; set; } = string.Empty; // único por carrera
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int Edad { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Race Race { get; set; } = null!;
    public RaceCategory Category { get; set; } = null!;
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
