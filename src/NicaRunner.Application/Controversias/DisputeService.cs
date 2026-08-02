using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Controversias.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Controversias;

public class DisputeService(
    ITimingDisputeRepository disputeRepository,
    IRaceRepository raceRepository,
    IResultRepository resultRepository,
    IResultAuditRepository auditRepository) : IDisputeService
{
    public async Task<List<TimingDisputeDto>> ListAsync(
        int raceId,
        DisputeEstado? estado = null,
        string? search = null,
        CancellationToken ct = default)
    {
        await GetRaceOrThrowAsync(raceId, ct);
        var rows = await disputeRepository.GetByRaceAsync(raceId, estado, search, ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<TimingDisputeDto> ResolveAsync(
        int raceId,
        int disputeId,
        ResolveDisputeRequest request,
        int actorUserId,
        UserRole actorRole,
        CancellationToken ct = default)
    {
        // ASSUMPTION: Lector read-only; solo Admin muta (HTTP authz en D2).
        if (actorRole != UserRole.Administrador)
            throw new ForbiddenException("Solo un administrador puede resolver controversias.");

        var race = await GetRaceOrThrowAsync(raceId, ct);
        var dispute = await disputeRepository.GetByIdAsync(raceId, disputeId, ct)
            ?? throw new NotFoundException($"No existe la controversia con id {disputeId} en la carrera {raceId}.");

        if (request.Estado == DisputeEstado.Oficial)
            await ApplyOfficialAsync(race, dispute, request, actorUserId, ct);

        dispute.Estado = request.Estado;
        dispute.UpdatedAt = DateTime.UtcNow;

        if (request.Estado == DisputeEstado.Oficial)
        {
            dispute.ResolvedByUserId = actorUserId;
            dispute.ResolvedAt = DateTime.UtcNow;
        }

        await disputeRepository.SaveChangesAsync(ct);
        return ToDto(dispute);
    }

    private async Task ApplyOfficialAsync(
        Race race,
        TimingDispute dispute,
        ResolveDisputeRequest request,
        int actorUserId,
        CancellationToken ct)
    {
        // Nunca Oficial silencioso: confirm + fuente con tiempo real.
        if (!request.ConfirmApplyOfficial)
            throw new ValidationException("Confirma aplicar el tiempo oficial antes de marcar Oficial.");

        if (request.SelectedSource is null)
            throw new ValidationException("Selecciona una fuente de tiempo (Chip, Checkpoint o Cámara).");

        var sourceSeconds = ResolveSourceSeconds(dispute, request.SelectedSource.Value);
        if (sourceSeconds is null)
            throw new ValidationException($"La fuente {request.SelectedSource} no tiene tiempo disponible.");

        if (dispute.ResultId is null)
            throw new ValidationException("La controversia no está vinculada a un resultado.");

        if (race.RaceStartUtc is null)
            throw new ValidationException("La carrera todavía no arrancó; no se puede aplicar un tiempo oficial.");

        var result = await resultRepository.GetByIdAsync(race.Id, dispute.ResultId.Value, ct)
            ?? throw new NotFoundException(
                $"No existe el resultado con id {dispute.ResultId.Value} en la carrera {race.Id}.");

        var nuevo = race.RaceStartUtc.Value.AddSeconds(sourceSeconds.Value);
        var anterior = result.TiempoLlegada;

        if (anterior != nuevo)
        {
            await auditRepository.AddAsync(new ResultAudit
            {
                ResultId = result.Id,
                AdminId = actorUserId,
                CampoModificado = "TiempoLlegada",
                ValorAnterior = anterior.ToString("O"),
                ValorNuevo = nuevo.ToString("O"),
                Razon = $"Controversia resuelta: {request.SelectedSource}"
            }, ct);

            result.TiempoLlegada = nuevo;
            result.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static double? ResolveSourceSeconds(TimingDispute dispute, TimingSource source) =>
        source switch
        {
            TimingSource.Chip => dispute.ChipSeconds,
            TimingSource.Checkpoint => dispute.CheckpointSeconds,
            TimingSource.Camera => dispute.CameraSeconds,
            _ => null
        };

    private async Task<Race> GetRaceOrThrowAsync(int raceId, CancellationToken ct) =>
        await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

    private static TimingDisputeDto ToDto(TimingDispute d) => new(
        d.Id,
        d.RaceId,
        d.ResultId,
        d.RunnerId,
        d.Dorsal,
        d.CorredorNombre,
        d.ChipSeconds,
        d.CheckpointSeconds,
        d.CameraSeconds,
        d.Estado,
        d.CapturistaNombre,
        d.CapturaNote,
        d.ResolvedByUserId,
        d.ResolvedAt,
        d.UpdatedAt);
}
