using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IRunnerRepository
{
    Task<Runner?> GetByIdAsync(int raceId, int runnerId, CancellationToken ct = default);
    Task<Runner?> GetByDorsalAsync(int raceId, string dorsal, CancellationToken ct = default);
    Task<List<Runner>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<List<Runner>> GetAllWithoutShareKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// design.md Decisión 3: lookup de un único corredor por su clave pública opaca
    /// (seek por índice único IX_Runners_PublicShareKey), con Race y Category
    /// precargadas para que el enlace público de detalle no necesite consultas extra.
    /// </summary>
    Task<Runner?> GetByShareKeyAsync(string shareKey, CancellationToken ct = default);
    Task<bool> DorsalExistsAsync(int raceId, string dorsal, int? excludeRunnerId = null, CancellationToken ct = default);
    Task<bool> ExistsByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<bool> ExistsByCategoryInRaceAsync(int raceId, int categoryId, CancellationToken ct = default);
    Task AddAsync(Runner runner, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Runner> runners, CancellationToken ct = default);
    void Remove(Runner runner);
    Task SaveChangesAsync(CancellationToken ct = default);
}
