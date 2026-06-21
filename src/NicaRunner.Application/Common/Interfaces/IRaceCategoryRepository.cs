using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IRaceCategoryRepository
{
    Task<RaceCategory?> GetByIdAsync(int raceId, int categoryId, CancellationToken ct = default);
    Task<List<RaceCategory>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task AddAsync(RaceCategory category, CancellationToken ct = default);
    void Remove(RaceCategory category);
    Task SaveChangesAsync(CancellationToken ct = default);
}
