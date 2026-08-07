using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class ControversyRepository(NicaRunnerDbContext context) : IControversyRepository
{
    // Abiertas primero, luego resueltas; dentro de cada grupo por fecha de
    // creación — el orden que espera la pantalla de Controversias.
    public async Task<List<Controversy>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        await context.Controversies
            .Where(c => c.RaceId == raceId)
            .OrderBy(c => c.Estado == "Abierta" ? 0 : 1)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<Controversy?> GetByIdAsync(int raceId, int controversyId, CancellationToken ct = default) =>
        await context.Controversies
            .FirstOrDefaultAsync(c => c.RaceId == raceId && c.Id == controversyId, ct);

    public async Task AddAsync(Controversy controversy, CancellationToken ct = default) =>
        await context.Controversies.AddAsync(controversy, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}
