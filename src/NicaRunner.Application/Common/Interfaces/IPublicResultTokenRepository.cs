using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IPublicResultTokenRepository
{
    Task<PublicResultToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<List<PublicResultToken>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task AddAsync(PublicResultToken token, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
