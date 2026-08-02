namespace NicaRunner.Application.Common.Interfaces;

/// <summary>
/// design.md Decisión 3: los cuatro agregados que necesita el enlace público de detalle
/// del corredor (posición y total de finalistas, por categoría y en toda la carrera),
/// resueltos por <see cref="IResultRepository.GetPlacingCountsAsync"/> en una sola
/// consulta agregada — nunca materializando los resultados de la carrera.
/// </summary>
public record PlacingCounts(int CategoryAhead, int CategoryTotal, int RaceAhead, int RaceTotal);
