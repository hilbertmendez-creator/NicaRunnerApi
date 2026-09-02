using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Results.Dtos;

public record ResultDto(
    int Id,
    int RaceId,
    int? RunnerId,
    string RunnerNombre,
    string? Dorsal,
    DateTime TiempoLlegada,
    int Posicion,
    int? CategoryId,
    string CategoriaNombre,
    int CapturistaId,
    string CapturistaNombre,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ResultEstado Estado,
    string? DorsalPropuesto,
    int? DisputeGroupId,
    DisputeMotivo? DisputeMotivo,
    long? ElapsedMillis = null,
    /// <summary>
    /// De dónde salió TiempoLlegada. Null solo en capturas anteriores a la columna — ver
    /// Result.TiempoOrigen. El backoffice lo usa para marcar los tiempos que no puso el
    /// reloj del servidor, igual que ya marca los ceros con StartOrigen.
    /// </summary>
    TiempoLlegadaOrigen? TiempoOrigen = null,
    int? TiempoOffsetConfianzaMs = null);
