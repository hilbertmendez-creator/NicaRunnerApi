using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface ITimingDisputeRepository
{
    Task<List<TimingDispute>> GetByRaceAsync(
        int raceId,
        DisputeEstado? estado = null,
        string? search = null,
        CancellationToken ct = default);

    Task<TimingDispute?> GetByIdAsync(int raceId, int disputeId, CancellationToken ct = default);

    Task AddAsync(TimingDispute dispute, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
