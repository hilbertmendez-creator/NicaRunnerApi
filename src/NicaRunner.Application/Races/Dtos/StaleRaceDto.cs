namespace NicaRunner.Application.Races.Dtos;

/// <summary>
/// stale-race-sweep: una carrera EnCurso sin actividad de captura reciente.
/// LastActivityUtc viene del último Result capturado o, si la carrera no tiene
/// ninguno todavía, de Race.RaceStartUtc.
/// </summary>
public record StaleRaceDto(int RaceId, string Nombre, DateTime LastActivityUtc);
