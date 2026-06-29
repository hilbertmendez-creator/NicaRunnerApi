using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<int> RevokeFamilyAsync(Guid familyId, RefreshTokenRevokedReason reason, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
