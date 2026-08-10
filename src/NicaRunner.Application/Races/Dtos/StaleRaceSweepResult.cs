namespace NicaRunner.Application.Races.Dtos;

/// <summary>stale-race-sweep: el resultado que el cron y el log de Admin ven — la lista completa, nunca solo un conteo.</summary>
public record StaleRaceSweepResult(List<StaleRaceDto> StaleRaces);
