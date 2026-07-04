using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IPublicResultTokenRepository
{
    Task<PublicResultToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<List<PublicResultToken>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task AddAsync(PublicResultToken token, CancellationToken ct = default);

    /// <summary>
    /// Borra físicamente los tokens públicos vencidos. Un token expirado ya
    /// no sirve para nada (ResolveValidTokenAsync lo rechaza igual), así que
    /// a diferencia de los refresh tokens revocados no hace falta ventana de
    /// retención forense. Se llama desde el endpoint admin de cleanup,
    /// disparado por cron externo.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTime now, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
