using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<int> RevokeFamilyAsync(Guid familyId, RefreshTokenRevokedReason reason, CancellationToken ct = default);

    /// <summary>
    /// Borra tokens ya inservibles: los que expiraron por TTL y los que
    /// fueron revocados hace más que <paramref name="revokedRetention"/>.
    /// La ventana de retención sobre revocados sirve para auditar replays
    /// recientes — pasado ese tiempo la cadena de rotación ya no importa.
    /// Retorna el número de filas borradas. Se llama desde el endpoint
    /// admin de cleanup, disparado por cron externo.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTime now, TimeSpan revokedRetention, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
