namespace NicaRunner.Domain.Entities;

public enum Sexo
{
    M,
    F
}

public class Runner
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string Dorsal { get; set; } = string.Empty; // único por carrera
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public Sexo? Sexo { get; set; }
    public string? Club { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public int Edad { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Race Race { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
