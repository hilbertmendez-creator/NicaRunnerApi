using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IRaceRepository
{
    Task<Race?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Race>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Race race, CancellationToken ct = default);
    void Remove(Race race);
    Task SaveChangesAsync(CancellationToken ct = default);
}
