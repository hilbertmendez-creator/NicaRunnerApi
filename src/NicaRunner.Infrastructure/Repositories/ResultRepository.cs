using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class ResultRepository(NicaRunnerDbContext context) : IResultRepository
{
    public Task<Result?> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default) =>
        context.Results.FirstOrDefaultAsync(r => r.RaceId == raceId && r.Id == resultId, ct);

    public Task<Result?> GetByIdAsync(int resultId, CancellationToken ct = default) =>
        context.Results.FirstOrDefaultAsync(r => r.Id == resultId, ct);

    public Task<List<Result>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.Results
            .Where(r => r.RaceId == raceId)
            .OrderBy(r => r.CategoryId)
            .ThenBy(r => r.Posicion)
            .ToListAsync(ct);

    public Task<List<Result>> GetAllByCategoryAsync(int raceId, int categoryId, CancellationToken ct = default) =>
        context.Results
            .Where(r => r.RaceId == raceId && r.CategoryId == categoryId)
            .ToListAsync(ct);

    public Task<bool> ExistsByRunnerAsync(int raceId, int runnerId, int? excludeResultId = null, CancellationToken ct = default) =>
        context.Results.AnyAsync(
            r => r.RaceId == raceId && r.RunnerId == runnerId && r.Id != excludeResultId,
            ct);

    public async Task AddAsync(Result result, CancellationToken ct = default) =>
        await context.Results.AddAsync(result, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
