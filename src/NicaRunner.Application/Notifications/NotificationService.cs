using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Notifications;

public class NotificationService(
    INotificationLogRepository logRepository,
    IResultRepository resultRepository,
    IRunnerRepository runnerRepository,
    IRaceRepository raceRepository,
    IEnumerable<INotificationSender> senders) : INotificationService
{
    private readonly Dictionary<NotificationChannel, INotificationSender> _sendersByChannel =
        senders.ToDictionary(s => s.Channel);

    public async Task<List<NotificationDto>> NotifyResultAsync(int resultId, CancellationToken ct = default)
    {
        var result = await resultRepository.GetByIdAsync(resultId, ct)
            ?? throw new NotFoundException($"No existe el resultado con id {resultId}.");

        var (race, runner) = await LoadContextAsync(result, ct);

        var logs = await SendForResultAsync(race, runner, result, ct);
        return logs.Select(ToDto).ToList();
    }

    public async Task<NotifyAllSummaryDto> NotifyAllAsync(int raceId, CancellationToken ct = default)
    {
        var race = await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

        var results = await resultRepository.GetAllByRaceAsync(raceId, ct);
        var runnersById = (await runnerRepository.GetAllByRaceAsync(raceId, ct)).ToDictionary(r => r.Id);

        var creadas = 0;
        var enviadas = 0;
        var fallidas = 0;

        foreach (var result in results)
        {
            if (result.RunnerId is not { } runnerId || !runnersById.TryGetValue(runnerId, out var runner))
                continue;

            var logs = await SendForResultAsync(race, runner, result, ct);
            creadas += logs.Count;
            enviadas += logs.Count(l => l.Status == NotificationStatus.Enviada);
            fallidas += logs.Count(l => l.Status == NotificationStatus.Fallida);
        }

        return new NotifyAllSummaryDto(results.Count, creadas, enviadas, fallidas);
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

    private async Task<List<NotificationLog>> SendForResultAsync(Race race, Runner runner, Result result, CancellationToken ct)
    {
        var mensaje = BuildMessage(race, runner, result);
        var channels = DetermineChannels(runner);

        if (channels.Count == 0)
        {
            var sinContacto = new NotificationLog
            {
                RaceId = race.Id,
                RunnerId = runner.Id,
                ResultId = result.Id,
                Channel = NotificationChannel.Email,
                Status = NotificationStatus.Fallida,
                Mensaje = mensaje,
                Error = "El corredor no tiene email ni teléfono registrados."
            };

            await logRepository.AddAsync(sinContacto, ct);
            await logRepository.SaveChangesAsync(ct);
            return [sinContacto];
        }

        var logs = new List<NotificationLog>();

        foreach (var (channel, destino) in channels)
        {
            var log = new NotificationLog
            {
                RaceId = race.Id,
                RunnerId = runner.Id,
                ResultId = result.Id,
                Channel = channel,
                Status = NotificationStatus.Pendiente,
                Mensaje = mensaje
            };

            await logRepository.AddAsync(log, ct);
            await logRepository.SaveChangesAsync(ct);

            if (_sendersByChannel.TryGetValue(channel, out var sender))
            {
                var sendResult = await sender.SendAsync(destino, mensaje, ct);
                log.Status = sendResult.Success ? NotificationStatus.Enviada : NotificationStatus.Fallida;
                log.Error = sendResult.ErrorMessage;
                log.SentAt = sendResult.Success ? DateTime.UtcNow : null;
            }
            else
            {
                log.Status = NotificationStatus.Fallida;
                log.Error = $"No hay un proveedor configurado para el canal {channel}.";
            }

            await logRepository.SaveChangesAsync(ct);
            logs.Add(log);
        }

        return logs;
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

    private static string BuildMessage(Race race, Runner runner, Result result) =>
        $"Hola {runner.Nombre}, tu resultado en {race.Nombre} fue: posición {result.Posicion}, " +
        $"tiempo {result.TiempoLlegada:HH:mm:ss}. ¡Gracias por participar!";

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
