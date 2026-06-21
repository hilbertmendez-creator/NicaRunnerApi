using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IRunnerRepository
{
    Task<Runner?> GetByIdAsync(int raceId, int runnerId, CancellationToken ct = default);
    Task<Runner?> GetByDorsalAsync(int raceId, string dorsal, CancellationToken ct = default);
    Task<List<Runner>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<bool> DorsalExistsAsync(int raceId, string dorsal, int? excludeRunnerId = null, CancellationToken ct = default);
    Task AddAsync(Runner runner, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Runner> runners, CancellationToken ct = default);
    void Remove(Runner runner);
    Task SaveChangesAsync(CancellationToken ct = default);
}
