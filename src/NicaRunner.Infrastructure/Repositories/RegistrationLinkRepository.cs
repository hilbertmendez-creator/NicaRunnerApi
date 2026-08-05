using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class RegistrationLinkRepository(NicaRunnerDbContext context) : IRegistrationLinkRepository
{
    public Task<RegistrationLink?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        context.RegistrationLinks.FirstOrDefaultAsync(l => l.Token == token, ct);

    public Task<RegistrationLink?> GetByIdAsync(int raceId, int linkId, CancellationToken ct = default) =>
        context.RegistrationLinks.FirstOrDefaultAsync(l => l.RaceId == raceId && l.Id == linkId, ct);

    public Task<List<RegistrationLink>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.RegistrationLinks
            .Where(l => l.RaceId == raceId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(RegistrationLink link, CancellationToken ct = default) =>
        await context.RegistrationLinks.AddAsync(link, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
