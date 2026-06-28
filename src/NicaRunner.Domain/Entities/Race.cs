namespace NicaRunner.Domain.Entities;

public enum RaceStatus
{
    Planeada,
    EnCurso,
    Terminada
}

public class Race
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaCarrera { get; set; }
    public RaceStatus Estado { get; set; } = RaceStatus.Planeada;
    public string JoinCode { get; set; } = string.Empty;
    public DateTime? RaceStartUtc { get; set; }
    public int AdminId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Admin { get; set; } = null!;
    public ICollection<RaceCategory> Categories { get; set; } = new List<RaceCategory>();
    public ICollection<Runner> Runners { get; set; } = new List<Runner>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
    public ICollection<PublicResultToken> PublicTokens { get; set; } = new List<PublicResultToken>();
    public ICollection<RaceJudge> Judges { get; set; } = new List<RaceJudge>();
}
