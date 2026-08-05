using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class RunnerRepository(NicaRunnerDbContext context) : IRunnerRepository
{
    public Task<Runner?> GetByIdAsync(int raceId, int runnerId, CancellationToken ct = default) =>
        context.Runners
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.RaceId == raceId && r.Id == runnerId, ct);

    public Task<Runner?> GetByDorsalAsync(int raceId, string dorsal, CancellationToken ct = default) =>
        context.Runners
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.RaceId == raceId && r.Dorsal == dorsal, ct);

    public Task<List<Runner>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.Runners
            .Include(r => r.Category)
            .Where(r => r.RaceId == raceId)
            .OrderBy(r => r.Dorsal)
            .ToListAsync(ct);

    // design.md Decisión 3: seek único por el índice filtrado IX_Runners_PublicShareKey.
    // Race y Category precargadas — el enlace público de detalle solo necesita 2
    // consultas más (GetByRunnerWithCategoryAsync + GetPlacingCountsAsync).
    public Task<Runner?> GetByShareKeyAsync(string shareKey, CancellationToken ct = default) =>
        context.Runners
            .Include(r => r.Race)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.PublicShareKey == shareKey, ct);

    // design.md Decisión 2: usada por RunnerShareKeyBackfillService. Filtra a nivel de
    // base (WHERE "PublicShareKey" IS NULL) en vez de traer toda la tabla y filtrar en
    // memoria — Runners puede crecer mucho más que Users a lo largo de varias carreras.
    public Task<List<Runner>> GetAllWithoutShareKeyAsync(CancellationToken ct = default) =>
        context.Runners
            .Where(r => r.PublicShareKey == null)
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

    // design.md D11: compara sobre DorsalNormalizado, no el string crudo — "21K7" y
    // "21K007" deben chocar igual. El índice único IX_Runners_RaceId_DorsalNormalizado
    // es el detector definitivo (cierra la ventana TOCTOU), pero este chequeo a nivel de
    // servicio es lo que produce un ConflictException legible en vez de un
    // DbUpdateException crudo al llegar a SaveChangesAsync.
    public Task<bool> DorsalExistsAsync(int raceId, string dorsal, int? excludeRunnerId = null, CancellationToken ct = default)
    {
        var normalized = DorsalNormalizer.Normalize(dorsal);
        return context.Runners.AnyAsync(
            r => r.RaceId == raceId && r.DorsalNormalizado == normalized && r.Id != excludeRunnerId,
            ct);
    }

    public Task<bool> ExistsByCategoryAsync(int categoryId, CancellationToken ct = default) =>
        context.Runners.AnyAsync(r => r.CategoryId == categoryId, ct);

    public Task<bool> ExistsByCategoryInRaceAsync(int raceId, int categoryId, CancellationToken ct = default) =>
        context.Runners.AnyAsync(r => r.RaceId == raceId && r.CategoryId == categoryId, ct);

    public async Task AddAsync(Runner runner, CancellationToken ct = default) =>
        await context.Runners.AddAsync(runner, ct);

    public async Task AddRangeAsync(IEnumerable<Runner> runners, CancellationToken ct = default) =>
        await context.Runners.AddRangeAsync(runners, ct);

    public void Remove(Runner runner) => context.Runners.Remove(runner);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
