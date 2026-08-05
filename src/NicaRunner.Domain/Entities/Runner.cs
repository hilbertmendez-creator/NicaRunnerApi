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

    /// <summary>
    /// design.md D11: forma normalizada de <see cref="Dorsal"/> (DorsalNormalizer.Normalize),
    /// calculada en cada escritura. La unicidad numérica real ("21K7" == "21K007") corre
    /// sobre esta columna vía el índice único IX_Runners_RaceId_DorsalNormalizado, aditivo
    /// al índice textual existente — un chequeo solo-en-servicio dejaría una ventana TOCTOU.
    /// </summary>
    public string DorsalNormalizado { get; set; } = string.Empty;

    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public Sexo? Sexo { get; set; }
    public string? Club { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public int Edad { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identificador opaco y permanente para el enlace público de detalle del corredor
    /// (GET /api/public/corredor/{PublicShareKey}). Nullable durante la ventana de
    /// backfill/deploy — ver RunnerShareKeyBackfillService y el índice único filtrado
    /// en NicaRunnerDbContext.
    /// </summary>
    public string? PublicShareKey { get; set; }

    public Race Race { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
