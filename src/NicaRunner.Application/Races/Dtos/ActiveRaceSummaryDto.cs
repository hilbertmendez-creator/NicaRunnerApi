namespace NicaRunner.Application.Races.Dtos;

// backoffice-user-status-toggle: DTO angosto para el pre-check de desactivación
// (design.md D6). No reutiliza RaceDto porque ese carga JoinCode — el secreto para
// unirse a una carrera como juez — sin motivo para exponerlo en un diálogo de confirmación.
public record ActiveRaceSummaryDto(int Id, string Nombre, DateTime FechaCarrera);
