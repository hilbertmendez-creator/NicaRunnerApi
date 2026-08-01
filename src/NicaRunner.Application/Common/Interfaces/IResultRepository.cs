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

    /// <summary>
    /// design.md Decisión 3: el resultado de un corredor en una carrera, con Category
    /// precargada (seek por el índice único filtrado IX_Results_RaceId_RunnerId).
    /// </summary>
    Task<Result?> GetByRunnerWithCategoryAsync(int raceId, int runnerId, CancellationToken ct = default);

    /// <summary>
    /// design.md Decisión 3: los cuatro agregados de posicionamiento (categoría y
    /// general, adelante y total) en UNA sola consulta agregada
    /// (GroupBy(_ => 1)), sobre IX_Results_RaceId_CategoryId_TiempoLlegada. Nunca
    /// materializa los resultados de la carrera completa.
    /// </summary>
    Task<PlacingCounts> GetPlacingCountsAsync(int raceId, int categoryId, DateTime tiempoLlegada, int resultId, CancellationToken ct = default);

    Task AddAsync(Result result, CancellationToken ct = default);

    /// <summary>
    /// Persiste un Result recién agregado a esta unidad de trabajo. Si choca
    /// con la UK (RaceId, IdempotencyKey) — caso de POSTs concurrentes con
    /// el mismo key — lanza IdempotencyConflictException para que el service
    /// re-lea el ganador. Si choca con la UK (RaceId, RunnerId) — dos capturas
    /// concurrentes del mismo corredor — lanza RunnerResultConflictException,
    /// que el service traduce a un conflicto real (no hay ganador que re-leer).
    /// Cualquier otra DbUpdateException se re-lanza tal cual (errores reales).
    /// </summary>
    Task SaveNewResultAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
