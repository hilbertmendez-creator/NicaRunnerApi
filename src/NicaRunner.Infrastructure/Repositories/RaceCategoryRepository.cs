using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class RaceCategoryRepository(NicaRunnerDbContext context) : IRaceCategoryRepository
{
    public Task<RaceCategory?> GetByIdAsync(int raceId, int categoryId, CancellationToken ct = default) =>
        context.RaceCategories.FirstOrDefaultAsync(c => c.RaceId == raceId && c.Id == categoryId, ct);

    public Task<List<RaceCategory>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.RaceCategories
            .Where(c => c.RaceId == raceId)
            .OrderBy(c => c.Orden)
            .ToListAsync(ct);

    public async Task AddAsync(RaceCategory category, CancellationToken ct = default) =>
        await context.RaceCategories.AddAsync(category, ct);

    public void Remove(RaceCategory category) => context.RaceCategories.Remove(category);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
