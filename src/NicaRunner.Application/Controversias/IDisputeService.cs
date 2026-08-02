using NicaRunner.Application.Controversias.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Controversias;

public interface IDisputeService
{
    Task<List<TimingDisputeDto>> ListAsync(
        int raceId,
        DisputeEstado? estado = null,
        string? search = null,
        CancellationToken ct = default);

    // Badge D3: Disputa + Revisión (excluye Oficial).
    Task<int> CountOpenAsync(int raceId, CancellationToken ct = default);

    Task<TimingDisputeDto> ResolveAsync(
        int raceId,
        int disputeId,
        ResolveDisputeRequest request,
        int actorUserId,
        UserRole actorRole,
        CancellationToken ct = default);
}
