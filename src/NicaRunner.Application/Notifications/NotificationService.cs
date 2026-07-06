using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications.Dtos;
using NicaRunner.Application.Notifications.EmailTemplates;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Notifications;

public class NotificationService(
    INotificationLogRepository logRepository,
    IResultRepository resultRepository,
    IRunnerRepository runnerRepository,
    IRaceRepository raceRepository,
    IEnumerable<INotificationSender> senders,
    IEmailTemplateRenderer emailTemplateRenderer) : INotificationService
{
    private readonly Dictionary<NotificationChannel, INotificationSender> _sendersByChannel =
        senders.ToDictionary(s => s.Channel);

    // Tope de reintentos para una notificación en Fallida — pasado esto,
    // ProcessPendingAsync deja de intentarla (ver GetPendingOrRetryableAsync).
    private const int MaxIntentos = 5;

    public async Task<List<NotificationDto>> NotifyResultAsync(int resultId, CancellationToken ct = default)
    {
        var result = await resultRepository.GetByIdAsync(resultId, ct)
            ?? throw new NotFoundException($"No existe el resultado con id {resultId}.");

        var (race, runner) = await LoadContextAsync(result, ct);

        // Un solo resultado: sigue enviando de inmediato, no tiene el riesgo
        // de timeout del caso masivo y el admin espera feedback al instante.
        var logs = await CreatePendingLogsAsync(race, runner, result, ct);
        foreach (var log in logs.Where(l => l.Status == NotificationStatus.Pendiente))
            await AttemptSendAsync(log, ct);

        return logs.Select(ToDto).ToList();
    }

    public async Task<NotifyAllSummaryDto> NotifyAllAsync(int raceId, CancellationToken ct = default)
    {
        var race = await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

        var results = await resultRepository.GetAllByRaceAsync(raceId, ct);
        var runnersById = (await runnerRepository.GetAllByRaceAsync(raceId, ct)).ToDictionary(r => r.Id);

        var creadas = 0;

        foreach (var result in results)
        {
            if (result.RunnerId is not { } runnerId || !runnersById.TryGetValue(runnerId, out var runner))
                continue;

            var logs = await CreatePendingLogsAsync(race, runner, result, ct);
            creadas += logs.Count;
        }

        // El envío real ocurre en ProcessPendingAsync (barrido periódico vía
        // cron admin) — acá solo se encola, para no bloquear el request con
        // cientos de llamadas HTTP secuenciales al proveedor de notificaciones.
        return new NotifyAllSummaryDto(results.Count, creadas, 0, 0);
    }

    public async Task<NotificationProcessSummaryDto> ProcessPendingAsync(CancellationToken ct = default)
    {
        var pending = await logRepository.GetPendingOrRetryableAsync(MaxIntentos, ct);

        var enviadas = 0;
        var fallidas = 0;

        foreach (var log in pending)
        {
            await AttemptSendAsync(log, ct);
            if (log.Status == NotificationStatus.Enviada)
                enviadas++;
            else
                fallidas++;
        }

        return new NotificationProcessSummaryDto(pending.Count, enviadas, fallidas);
    }

    public async Task<NotificationDto> GetStatusAsync(int notificationId, CancellationToken ct = default)
    {
        var log = await logRepository.GetByIdAsync(notificationId, ct)
            ?? throw new NotFoundException($"No existe la notificación con id {notificationId}.");

        return ToDto(log);
    }

    private async Task<(Race Race, Runner Runner)> LoadContextAsync(Result result, CancellationToken ct)
    {
        var race = await raceRepository.GetByIdAsync(result.RaceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {result.RaceId}.");

        var runnerId = result.RunnerId
            ?? throw new ConflictException("Este resultado todavía no tiene un dorsal asignado; no se puede notificar.");
        var runner = await runnerRepository.GetByIdAsync(result.RaceId, runnerId, ct)
            ?? throw new NotFoundException($"No existe el corredor con id {runnerId}.");

        return (race, runner);
    }

    // Crea los NotificationLog en Pendiente para un resultado (uno por canal
    // con contacto disponible) sin enviar nada — el envío real lo hace
    // AttemptSendAsync, ya sea de inmediato (NotifyResultAsync) o en el
    // barrido (ProcessPendingAsync).
    private async Task<List<NotificationLog>> CreatePendingLogsAsync(Race race, Runner runner, Result result, CancellationToken ct)
    {
        var rendered = BuildRenderedEmail(race, runner, result);
        var mensaje = rendered.Text;
        var channels = DetermineChannels(runner);

        if (channels.Count == 0)
        {
            var sinContacto = new NotificationLog
            {
                RaceId = race.Id,
                RunnerId = runner.Id,
                ResultId = result.Id,
                Runner = runner,
                Channel = NotificationChannel.Email,
                Status = NotificationStatus.Fallida,
                Mensaje = mensaje,
                Error = "El corredor no tiene email ni teléfono registrados.",
                // Agotado a propósito: esta falla no se resuelve reintentando.
                IntentosEnvio = MaxIntentos
            };

            await logRepository.AddAsync(sinContacto, ct);
            await logRepository.SaveChangesAsync(ct);
            return [sinContacto];
        }

        var logs = new List<NotificationLog>();

        foreach (var (channel, _) in channels)
        {
            var log = new NotificationLog
            {
                RaceId = race.Id,
                RunnerId = runner.Id,
                ResultId = result.Id,
                Runner = runner,
                Channel = channel,
                Status = NotificationStatus.Pendiente,
                Mensaje = mensaje
            };

            await logRepository.AddAsync(log, ct);
            logs.Add(log);
        }

        await logRepository.SaveChangesAsync(ct);
        return logs;
    }

    // Intenta enviar un log en Pendiente o Fallida-reintentable. El destino
    // (email/teléfono) se resuelve del corredor en el momento del envío, no
    // se guarda en el log — así una carrera vieja no arrastra un contacto
    // desactualizado si el corredor lo corrigió después de la captura.
    private async Task AttemptSendAsync(NotificationLog log, CancellationToken ct)
    {
        log.IntentosEnvio++;

        var destino = log.Channel switch
        {
            NotificationChannel.Email => log.Runner.Email,
            NotificationChannel.WhatsApp => log.Runner.Telefono,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(destino))
        {
            log.Status = NotificationStatus.Fallida;
            log.Error = "El corredor ya no tiene un destino válido para este canal.";
        }
        else if (_sendersByChannel.TryGetValue(log.Channel, out var sender))
        {
            NotificationSendResult sendResult;
            if (log.Channel == NotificationChannel.Email)
            {
                var rendered = await TryRebuildRenderedEmailAsync(log, ct);
                if (rendered is null)
                {
                    log.Status = NotificationStatus.Fallida;
                    log.Error = "No se pudo reconstruir el contenido del correo para reenvío.";
                }
                else
                {
                    sendResult = await sender.SendAsync(destino, rendered.Text, rendered.Subject, rendered.Html, ct);
                    log.Status = sendResult.Success ? NotificationStatus.Enviada : NotificationStatus.Fallida;
                    log.Error = sendResult.ErrorMessage;
                    log.SentAt = sendResult.Success ? DateTime.UtcNow : null;
                }
            }
            else
            {
                sendResult = await sender.SendAsync(destino, log.Mensaje, ct: ct);
                log.Status = sendResult.Success ? NotificationStatus.Enviada : NotificationStatus.Fallida;
                log.Error = sendResult.ErrorMessage;
                log.SentAt = sendResult.Success ? DateTime.UtcNow : null;
            }
        }
        else
        {
            log.Status = NotificationStatus.Fallida;
            log.Error = $"No hay un proveedor configurado para el canal {log.Channel}.";
        }

        await logRepository.SaveChangesAsync(ct);
    }

    private static List<(NotificationChannel Channel, string Destino)> DetermineChannels(Runner runner)
    {
        var channels = new List<(NotificationChannel Channel, string Destino)>();

        if (!string.IsNullOrWhiteSpace(runner.Email))
            channels.Add((NotificationChannel.Email, runner.Email));

        if (!string.IsNullOrWhiteSpace(runner.Telefono))
            channels.Add((NotificationChannel.WhatsApp, runner.Telefono));

        return channels;
    }

    private RenderedEmail BuildRenderedEmail(Race race, Runner runner, Result result)
    {
        var model = new RaceResultEmailModel(runner.Nombre, race.Nombre, result.Posicion, result.TiempoLlegada);
        return emailTemplateRenderer.RenderRaceResult(model);
    }

    private async Task<RenderedEmail?> TryRebuildRenderedEmailAsync(NotificationLog log, CancellationToken ct)
    {
        if (log.ResultId <= 0)
            return null;

        var result = await resultRepository.GetByIdAsync(log.ResultId, ct);
        if (result is null)
            return null;

        try
        {
            var (race, runner) = await LoadContextAsync(result, ct);
            return BuildRenderedEmail(race, runner, result);
        }
        catch (NotFoundException)
        {
            return null;
        }
        catch (ConflictException)
        {
            return null;
        }
    }

    private static NotificationDto ToDto(NotificationLog log) => new(
        log.Id,
        log.RaceId,
        log.RunnerId,
        log.ResultId,
        log.Channel,
        log.Status,
        log.Mensaje,
        log.Error,
        log.CreatedAt,
        log.SentAt);
}
