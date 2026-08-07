using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class RaceCategoryRepository(NicaRunnerDbContext context) : IRaceCategoryRepository
{
    public Task<Category?> GetByIdAsync(int raceId, int categoryId, CancellationToken ct = default) =>
        context.RaceCategories
            .Where(rc => rc.RaceId == raceId && rc.CategoryId == categoryId)
            .Select(rc => rc.Category)
            .FirstOrDefaultAsync(ct);

    public Task<List<Category>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.RaceCategories
            .Where(rc => rc.RaceId == raceId)
            .Select(rc => rc.Category)
            .OrderBy(c => c.Orden)
            .ToListAsync(ct);

    public Task<bool> IsSelectedAsync(int raceId, int categoryId, CancellationToken ct = default) =>
        context.RaceCategories.AnyAsync(rc => rc.RaceId == raceId && rc.CategoryId == categoryId, ct);

    public async Task SelectAsync(int raceId, int categoryId, CancellationToken ct = default) =>
        await context.RaceCategories.AddAsync(new RaceCategory { RaceId = raceId, CategoryId = categoryId }, ct);

    public Task<RaceCategory?> GetAssociationAsync(int raceId, int categoryId, CancellationToken ct = default) =>
        context.RaceCategories.FirstOrDefaultAsync(rc => rc.RaceId == raceId && rc.CategoryId == categoryId, ct);

    public Task<List<RaceCategory>> GetAssociationsByRaceAsync(int raceId, CancellationToken ct = default) =>
        BaseQuery()
            .Where(rc => rc.RaceId == raceId)
            .ToListAsync(ct);

    public Task<List<RaceCategory>> GetAssociationsByIdsAsync(
        int raceId, IReadOnlyCollection<int> categoryIds, CancellationToken ct = default) =>
        BaseQuery()
            .Where(rc => rc.RaceId == raceId && categoryIds.Contains(rc.CategoryId))
            .ToListAsync(ct);

    // SIN AsNoTracking, a propósito: el servicio muta estas entidades y después vuelve a
    // pedir todas las de la carrera para recalcular Race.Estado. La resolución de
    // identidad de EF devuelve las MISMAS instancias ya mutadas — con AsNoTracking
    // recalcularíamos sobre datos viejos y el estado de la carrera quedaría atrasado.
    private IQueryable<RaceCategory> BaseQuery() =>
        context.RaceCategories
            .Include(rc => rc.Category)
            .Include(rc => rc.StartedBy)
            .Include(rc => rc.ClosedBy)
            .OrderBy(rc => rc.Category.Orden);

    public void Remove(RaceCategory association) => context.RaceCategories.Remove(association);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
