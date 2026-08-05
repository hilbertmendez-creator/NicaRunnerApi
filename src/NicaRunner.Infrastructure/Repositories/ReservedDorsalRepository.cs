using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

// design.md D8/D9/D11: IsReservedAsync compara sobre DorsalNormalizado (nunca el string
// crudo) — igual criterio que RunnerRepository.DorsalExistsAsync, mismo grano (RaceId).
public class ReservedDorsalRepository(NicaRunnerDbContext context) : IReservedDorsalRepository
{
    public Task<bool> IsReservedAsync(int raceId, string dorsal, CancellationToken ct = default)
    {
        var normalized = DorsalNormalizer.Normalize(dorsal);
        return context.ReservedDorsals.AnyAsync(d => d.RaceId == raceId && d.DorsalNormalizado == normalized, ct);
    }

    public Task<List<ReservedDorsal>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.ReservedDorsals
            .Where(d => d.RaceId == raceId)
            .OrderBy(d => d.Dorsal)
            .ToListAsync(ct);

    public Task<ReservedDorsal?> GetByIdAsync(int raceId, int reservedDorsalId, CancellationToken ct = default) =>
        context.ReservedDorsals.FirstOrDefaultAsync(d => d.RaceId == raceId && d.Id == reservedDorsalId, ct);

    public async Task AddAsync(ReservedDorsal reservedDorsal, CancellationToken ct = default) =>
        await context.ReservedDorsals.AddAsync(reservedDorsal, ct);

    public void Remove(ReservedDorsal reservedDorsal) => context.ReservedDorsals.Remove(reservedDorsal);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
