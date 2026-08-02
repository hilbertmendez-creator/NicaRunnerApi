using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class TimingDisputeRepository(NicaRunnerDbContext context) : ITimingDisputeRepository
{
    public async Task<List<TimingDispute>> GetByRaceAsync(
        int raceId,
        DisputeEstado? estado = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var query = context.TimingDisputes.AsQueryable().Where(d => d.RaceId == raceId);

        if (estado is not null)
            query = query.Where(d => d.Estado == estado);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                (d.Dorsal != null && d.Dorsal.Contains(term)) ||
                d.CorredorNombre.Contains(term));
        }

        return await query
            .OrderBy(d => d.Dorsal)
            .ThenBy(d => d.Id)
            .ToListAsync(ct);
    }

    public Task<TimingDispute?> GetByIdAsync(int raceId, int disputeId, CancellationToken ct = default) =>
        context.TimingDisputes.FirstOrDefaultAsync(d => d.RaceId == raceId && d.Id == disputeId, ct);

    public async Task AddAsync(TimingDispute dispute, CancellationToken ct = default) =>
        await context.TimingDisputes.AddAsync(dispute, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
