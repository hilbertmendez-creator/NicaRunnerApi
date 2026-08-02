namespace NicaRunner.Domain.Entities;

public enum DisputeEstado
{
    Revision = 0,
    Disputa = 1,
    Oficial = 2
}

// Fuente al cerrar Oficial. Chip/Cámara nullable en MVP (sin hardware).
public enum TimingSource
{
    Chip = 0,
    Checkpoint = 1,
    Camera = 2
}

// Controversia multi-fuente; el tiempo oficial se aplica sobre Result.
public class TimingDispute
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int? ResultId { get; set; }
    public int? RunnerId { get; set; }
    public string? Dorsal { get; set; }
    public string CorredorNombre { get; set; } = string.Empty;

    // Segundos desde arranque (grid UI). Null = sin dato / placeholder.
    public double? ChipSeconds { get; set; }
    public double? CheckpointSeconds { get; set; }
    public double? CameraSeconds { get; set; }

    public DisputeEstado Estado { get; set; } = DisputeEstado.Revision;
    public string? CapturistaNombre { get; set; }
    public string? CapturaNote { get; set; }
    public int? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Race Race { get; set; } = null!;
    public Result? Result { get; set; }
    public Runner? Runner { get; set; }
    public User? ResolvedBy { get; set; }
}
