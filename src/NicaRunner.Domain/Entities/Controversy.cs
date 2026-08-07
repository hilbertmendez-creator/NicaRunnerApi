namespace NicaRunner.Domain.Entities;

/// <summary>
/// Disputa de tiempo de un corredor en una carrera. La app de captura
/// (fuera de alcance) alimenta la tabla con los tres tiempos; el backoffice
/// lista y resuelve cada disputa. Estado: "Abierta" | "Resuelta".
/// </summary>
public class Controversy
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public string Dorsal { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public double? TiempoChip { get; set; }
    public double? TiempoCaptura { get; set; }
    public double? TiempoCamara { get; set; }
    public double? Diferencia { get; set; }
    public string Estado { get; set; } = "Abierta"; // Abierta | Resuelta
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public Race Race { get; set; } = null!;
}
