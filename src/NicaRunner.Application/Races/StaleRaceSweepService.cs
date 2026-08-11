using Microsoft.Extensions.Logging;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Races;

// Guardia anti-zombie (design.md decisiones 1-5): una carrera EnCurso nunca debería
// quedar olvidada para siempre. Este servicio SOLO lee y notifica — nunca escribe
// Race.Estado ni ninguna otra columna. Un humano decide si cerrarla.
public class StaleRaceSweepService(
    IRaceRepository raceRepository,
    IResultRepository resultRepository,
    IUserRepository userRepository,
    IEnumerable<INotificationSender> notificationSenders,
    IAdminNotificationService adminNotificationService,
    ILogger<StaleRaceSweepService> logger) : IStaleRaceSweepService
{
    // Decisión 2: valor fijo, no configurable por appsettings.
    public static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(12);

    public async Task<StaleRaceSweepResult> RunAsync(CancellationToken ct = default)
    {
        // Decisión 5: solo EnCurso es candidata — Planeada y Terminada quedan
        // estructuralmente afuera de esta consulta, nunca se evalúan ni se reportan.
        var candidates = await raceRepository.GetByStatusAsync(RaceStatus.EnCurso, ct);
        if (candidates.Count == 0)
            return new StaleRaceSweepResult([]);

        var lastCaptureAtByRaceId = await resultRepository.GetLastCaptureAtByRaceIdsAsync(
            candidates.Select(r => r.Id).ToList(), ct);

        var now = DateTime.UtcNow;
        var staleRaces = new List<StaleRaceDto>();

        foreach (var race in candidates)
        {
            // Decisión 1: MAX(Result.CreatedAt) si hay capturas; si no, Race.RaceStartUtc.
            // Una EnCurso sin RaceStartUtc (no debería pasar — StartAsync siempre lo
            // setea) no tiene ancla temporal confiable: se salta en vez de adivinar.
            DateTime? lastActivity = lastCaptureAtByRaceId.TryGetValue(race.Id, out var lastCaptureAt)
                ? lastCaptureAt
                : race.RaceStartUtc;

            if (lastActivity is null)
                continue;

            if (now - lastActivity.Value >= StaleThreshold)
                staleRaces.Add(new StaleRaceDto(race.Id, race.Nombre, lastActivity.Value));
        }

        if (staleRaces.Count > 0)
        {
            // Dos canales INDEPENDIENTES: la campana no depende de que haya un sender de
            // email configurado ni de que existan destinatarios Administrador.
            await RecordAdminNotificationBestEffortAsync(staleRaces, ct);
            await NotifyAdminsBestEffortAsync(staleRaces, ct);
        }

        return new StaleRaceSweepResult(staleRaces);
    }

    // Un resumen POR BARRIDO, no una fila por carrera: el cron corre seguido y una carrera
    // olvidada sigue estando olvidada en la corrida siguiente. RaceId solo cuando hay una
    // sola candidata — con varias el aviso no apunta a ninguna en particular.
    private async Task RecordAdminNotificationBestEffortAsync(List<StaleRaceDto> staleRaces, CancellationToken ct)
    {
        try
        {
            var nombres = string.Join(", ", staleRaces.Select(r => $"\"{r.Nombre}\""));
            var mensaje = staleRaces.Count == 1
                ? $"La carrera {nombres} sigue EnCurso sin capturas recientes. Revisá si ya terminó."
                : $"{staleRaces.Count} carreras siguen EnCurso sin capturas recientes: {nombres}.";

            await adminNotificationService.NotifyAsync(
                AdminNotificationType.CarrerasSinActividad,
                mensaje,
                staleRaces.Count == 1 ? staleRaces[0].RaceId : null,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex, "Error registrando en la bandeja el barrido de {Count} carrera(s) sin actividad.", staleRaces.Count);
        }
    }

    // Best-effort a propósito, mismo criterio que RaceService.NotifyAdminsBestEffortAsync:
    // el barrido SIEMPRE devuelve 200 con la lista, notificar es un plus — un proveedor de
    // email caído nunca puede esconder que hay carreras potencialmente abandonadas.
    private async Task NotifyAdminsBestEffortAsync(List<StaleRaceDto> staleRaces, CancellationToken ct)
    {
        try
        {
            var admins = await userRepository.GetByRoleAsync(UserRole.Administrador, ct);
            if (admins.Count == 0)
                return;

            var sender = notificationSenders.FirstOrDefault(s => s.Channel == NotificationChannel.Email);
            if (sender is null)
            {
                logger.LogWarning(
                    "No hay un sender de Email configurado; no se notificó el barrido de {Count} carrera(s) sin actividad.",
                    staleRaces.Count);
                return;
            }

            var (subject, body) = StaleRaceAlertEmail.Build(staleRaces);

            foreach (var admin in admins)
            {
                var result = await sender.SendAsync(admin.Email, body, subject, html: null, ct);
                if (!result.Success)
                    logger.LogWarning(
                        "No se pudo notificar el barrido de carreras sin actividad a {AdminEmail}: {Error}",
                        admin.Email, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error notificando el barrido de {Count} carrera(s) sin actividad.", staleRaces.Count);
        }
    }
}
