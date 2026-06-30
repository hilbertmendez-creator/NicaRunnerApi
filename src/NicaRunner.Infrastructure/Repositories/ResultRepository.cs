using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class ResultRepository(NicaRunnerDbContext context) : IResultRepository
{
    public Task<Result?> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default) =>
        context.Results
            .Include(r => r.Runner)
            .Include(r => r.Category)
            .Include(r => r.Capturista)
            .FirstOrDefaultAsync(r => r.RaceId == raceId && r.Id == resultId, ct);

    public Task<Result?> GetByIdAsync(int resultId, CancellationToken ct = default) =>
        context.Results
            .Include(r => r.Runner)
            .Include(r => r.Category)
            .Include(r => r.Capturista)
            .FirstOrDefaultAsync(r => r.Id == resultId, ct);

    public Task<List<Result>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.Results
            .Include(r => r.Runner)
            .Include(r => r.Category)
            .Include(r => r.Capturista)
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

    public Task<Result?> GetByIdempotencyKeyAsync(int raceId, string idempotencyKey, CancellationToken ct = default) =>
        context.Results
            .Include(r => r.Runner)
            .Include(r => r.Category)
            .Include(r => r.Capturista)
            .FirstOrDefaultAsync(r => r.RaceId == raceId && r.IdempotencyKey == idempotencyKey, ct);

    public async Task AddAsync(Result result, CancellationToken ct = default) =>
        await context.Results.AddAsync(result, ct);

    public async Task SaveNewResultAsync(CancellationToken ct = default)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsIdempotencyKeyViolation(ex))
        {
            throw new IdempotencyConflictException();
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);

    // Tanto Npgsql como SqliteException incluyen el nombre de la columna en
    // su mensaje de violación de UK. Como en este punto del flujo el único
    // UK candidato a violarse es (RaceId, IdempotencyKey), basta con detectar
    // la palabra "IdempotencyKey" en la cadena. No es lo más bonito (depende
    // del mensaje del provider), pero es el método portable más simple sin
    // tipar contra Npgsql/Sqlite específicamente.
    private static bool IsIdempotencyKeyViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase) == true;
}
