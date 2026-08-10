using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Dtos;
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

    public async Task<PaginatedList<Result>> GetPaginatedByRaceAsync(int raceId, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        var query = context.Results.Where(r => r.RaceId == raceId);
        var total = await query.CountAsync(ct);
        var items = await query
            .Include(r => r.Runner)
            .Include(r => r.Category)
            .Include(r => r.Capturista)
            .OrderBy(r => r.CategoryId)
            .ThenBy(r => r.Posicion)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
            
        return new PaginatedList<Result>(items, total);
    }

    public Task<List<Result>> GetAllByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.Results
            .Include(r => r.Runner)
            .Include(r => r.Category)
            .Include(r => r.Capturista)
            .Where(r => r.RaceId == raceId)
            .OrderBy(r => r.CategoryId)
            .ThenBy(r => r.Posicion)
            .ToListAsync(ct);

    public Task<List<Result>> GetDisputedByRaceAsync(int raceId, CancellationToken ct = default) =>
        context.Results
            .Include(r => r.Runner)
            .Include(r => r.Category)
            .Include(r => r.Capturista)
            .Where(r => r.RaceId == raceId && r.Estado == ResultEstado.Controversia)
            .OrderBy(r => r.DisputeGroupId).ThenBy(r => r.TiempoLlegada)
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

    // design.md Decisión 3: seek único por el índice filtrado IX_Results_RaceId_RunnerId.
    public Task<Result?> GetByRunnerWithCategoryAsync(int raceId, int runnerId, CancellationToken ct = default) =>
        context.Results
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.RaceId == raceId && r.RunnerId == runnerId, ct);

    // design.md Decisión 3: los cuatro agregados en UNA sola consulta —
    // GroupBy(_ => 1) traduce a un solo SELECT con COUNT(...) FILTER/CASE por columna,
    // sin materializar ninguna fila de Result. Desempate (TiempoLlegada, Id) idéntico al
    // de RecalculatePositionsAsync (ResultService.cs) para que la posición derivada acá
    // nunca contradiga la Posicion guardada.
    public async Task<PlacingCounts> GetPlacingCountsAsync(int raceId, int categoryId, DateTime tiempoLlegada, int resultId, CancellationToken ct = default)
    {
        // Sin r.CategoryId != null acá: esa condición pertenece solo a los agregados de
        // categoría (ya filtrados abajo por r.CategoryId == categoryId, que por sí solo
        // excluye las filas sin categoría). Aplicarla también a la base excluía la propia
        // fila del resultado consultado de RaceTotal cuando su CategoryId es null, mientras
        // otras filas categorizadas seguían contando en RaceAhead — PosicionGeneral podía
        // superar TotalGeneral (design.md Decisión 7).
        var counts = await context.Results
            .Where(r => r.RaceId == raceId && r.RunnerId != null)
            .GroupBy(_ => 1)
            .Select(g => new PlacingCounts(
                g.Count(r => r.CategoryId == categoryId &&
                    (r.TiempoLlegada < tiempoLlegada || (r.TiempoLlegada == tiempoLlegada && r.Id < resultId))),
                g.Count(r => r.CategoryId == categoryId),
                g.Count(r =>
                    r.TiempoLlegada < tiempoLlegada || (r.TiempoLlegada == tiempoLlegada && r.Id < resultId)),
                g.Count()))
            .FirstOrDefaultAsync(ct);

        // Sin filas en la carrera (caso imposible en la práctica — este método solo se
        // llama cuando ya se resolvió un Result existente) o carrera vacía: agregados en
        // cero en vez de lanzar.
        return counts ?? new PlacingCounts(0, 0, 0, 0);
    }

    // race-close: los dos agregados que bloquean el cierre en UNA sola consulta
    // (GroupBy(_ => 1), mismo precedente que GetPlacingCountsAsync). MissingRunner
    // EXCLUYE Anulado a propósito: una captura anulada sin dorsal nunca va a recibir
    // uno, así que contarla bloquearía el cierre para siempre.
    public async Task<RaceCloseBlockerCounts> GetCloseBlockerCountsAsync(int raceId, CancellationToken ct = default)
    {
        var counts = await context.Results
            .Where(r => r.RaceId == raceId)
            .GroupBy(_ => 1)
            .Select(g => new RaceCloseBlockerCounts(
                g.Count(r => r.Estado == ResultEstado.Controversia),
                g.Count(r => r.Estado != ResultEstado.Anulado && r.RunnerId == null)))
            .FirstOrDefaultAsync(ct);

        return counts == default ? new RaceCloseBlockerCounts(0, 0) : counts;
    }

    // stale-race-sweep: MAX(CreatedAt) por RaceId en UNA sola consulta agregada, sin
    // materializar Results — mismo precedente que GetPlacingCountsAsync/
    // GetCloseBlockerCountsAsync. Sin Where por Estado a propósito: Anulado también
    // cuenta como actividad humana reciente (design.md decisión 1).
    public async Task<Dictionary<int, DateTime>> GetLastCaptureAtByRaceIdsAsync(
        IReadOnlyCollection<int> raceIds, CancellationToken ct = default)
    {
        if (raceIds.Count == 0)
            return new Dictionary<int, DateTime>();

        return await context.Results
            .Where(r => raceIds.Contains(r.RaceId))
            .GroupBy(r => r.RaceId)
            .Select(g => new { RaceId = g.Key, LastCaptureAt = g.Max(r => r.CreatedAt) })
            .ToDictionaryAsync(x => x.RaceId, x => x.LastCaptureAt, ct);
    }

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
        catch (DbUpdateException ex) when (IsRunnerResultViolation(ex))
        {
            throw new RunnerResultConflictException();
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

    // Mismo truco portable que IsIdempotencyKeyViolation: tanto el nombre de
    // índice que genera Postgres (IX_Results_RaceId_RunnerId) como el mensaje
    // nativo de Sqlite (que lista los nombres de columna) contienen "RunnerId".
    private static bool IsRunnerResultViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("RunnerId", StringComparison.OrdinalIgnoreCase) == true;
}
