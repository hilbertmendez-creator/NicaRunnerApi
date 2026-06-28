using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class RaceRepository(NicaRunnerDbContext context) : IRaceRepository
{
    public Task<Race?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Races.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Race?> GetByJoinCodeAsync(string joinCode, CancellationToken ct = default) =>
        context.Races.FirstOrDefaultAsync(r => r.JoinCode == joinCode, ct);

    public Task<List<Race>> GetAllAsync(CancellationToken ct = default) =>
        context.Races.OrderByDescending(r => r.FechaCarrera).ToListAsync(ct);

    public Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken ct = default) =>
        context.Races.AnyAsync(r => r.JoinCode == joinCode, ct);

    public async Task AddAsync(Race race, CancellationToken ct = default) =>
        await context.Races.AddAsync(race, ct);

    public async Task AddJudgeAsync(RaceJudge judge, CancellationToken ct = default) =>
        await context.RaceJudges.AddAsync(judge, ct);

    public Task<bool> IsJudgeAsync(int raceId, int userId, CancellationToken ct = default) =>
        context.RaceJudges.AnyAsync(j => j.RaceId == raceId && j.UserId == userId, ct);

    public void Remove(Race race) => context.Races.Remove(race);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
