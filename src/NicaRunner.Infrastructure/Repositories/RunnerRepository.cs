using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class RunnerRepository(NicaRunnerDbContext context) : IRunnerRepository
{
    public Task<Runner?> GetByIdAsync(int raceId, int runnerId, CancellationToken ct = default) =>
        context.Runners.FirstOrDefaultAsync(r => r.RaceId == raceId && r.Id == runnerId, ct);

    public Task<Runner?> GetByDorsalAsync(int raceId, string dorsal, CancellationToken ct = default) =>
        context.Runners.FirstOrDefaultAsync(r => r.RaceId == raceId && r.Dorsal == dorsal, ct);

    public Task<List<Runner>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.Runners
            .Where(r => r.RaceId == raceId)
            .OrderBy(r => r.Dorsal)
            .ToListAsync(ct);

    public Task<bool> DorsalExistsAsync(int raceId, string dorsal, int? excludeRunnerId = null, CancellationToken ct = default) =>
        context.Runners.AnyAsync(
            r => r.RaceId == raceId && r.Dorsal == dorsal && r.Id != excludeRunnerId,
            ct);

    public async Task AddAsync(Runner runner, CancellationToken ct = default) =>
        await context.Runners.AddAsync(runner, ct);

    public void Remove(Runner runner) => context.Runners.Remove(runner);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
