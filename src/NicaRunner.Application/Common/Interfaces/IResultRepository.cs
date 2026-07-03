using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IResultRepository
{
    Task<Result?> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default);
    Task<Result?> GetByIdAsync(int resultId, CancellationToken ct = default);
    Task<List<Result>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<List<Result>> GetAllByCategoryAsync(int raceId, int categoryId, CancellationToken ct = default);
    Task<bool> ExistsByRunnerAsync(int raceId, int runnerId, int? excludeResultId = null, CancellationToken ct = default);
    Task<Result?> GetByIdempotencyKeyAsync(int raceId, string idempotencyKey, CancellationToken ct = default);
    Task AddAsync(Result result, CancellationToken ct = default);

    /// <summary>
    /// Persiste un Result recién agregado a esta unidad de trabajo. Si choca
    /// con la UK (RaceId, IdempotencyKey) — caso de POSTs concurrentes con
    /// el mismo key — lanza IdempotencyConflictException para que el service
    /// re-lea el ganador. Cualquier otra DbUpdateException se re-lanza tal
    /// cual (errores reales).
    /// </summary>
    Task SaveNewResultAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
