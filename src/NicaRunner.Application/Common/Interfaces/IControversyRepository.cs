using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IControversyRepository
{
    Task<List<Controversy>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<Controversy?> GetByIdAsync(int raceId, int controversyId, CancellationToken ct = default);
    Task AddAsync(Controversy controversy, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
