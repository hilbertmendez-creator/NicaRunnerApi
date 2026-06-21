using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class PublicResultTokenRepository(NicaRunnerDbContext context) : IPublicResultTokenRepository
{
    public Task<PublicResultToken?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        context.PublicResultTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public Task<List<PublicResultToken>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.PublicResultTokens
            .Where(t => t.RaceId == raceId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(PublicResultToken token, CancellationToken ct = default) =>
        await context.PublicResultTokens.AddAsync(token, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
