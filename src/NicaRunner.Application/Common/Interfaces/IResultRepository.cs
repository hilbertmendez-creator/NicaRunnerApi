using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IResultRepository
{
    Task<Result?> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default);
    Task<List<Result>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<List<Result>> GetAllByCategoryAsync(int raceId, int categoryId, CancellationToken ct = default);
    Task<bool> ExistsByRunnerAsync(int raceId, int runnerId, int? excludeResultId = null, CancellationToken ct = default);
    Task AddAsync(Result result, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
