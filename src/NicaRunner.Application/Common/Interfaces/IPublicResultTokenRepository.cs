using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IPublicResultTokenRepository
{
    Task<PublicResultToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<List<PublicResultToken>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);

    /// <summary>
    /// Busca un token por su id dentro de una carrera. El filtro por raceId no
    /// es cosmético: las rutas admin son /api/races/{raceId}/public-token/{tokenId}
    /// y sin él un id suelto alcanzaría para tocar el token de otra carrera.
    /// </summary>
    Task<PublicResultToken?> GetByIdAsync(int raceId, int tokenId, CancellationToken ct = default);

    Task AddAsync(PublicResultToken token, CancellationToken ct = default);

    /// <summary>
    /// Borra físicamente los tokens públicos que ya no sirven: los vencidos por
    /// fecha y los revocados a mano (IsExpired). ResolveValidTokenAsync rechaza
    /// ambos igual, así que —a diferencia de los refresh tokens revocados— no
    /// hace falta ventana de retención forense. Se llama desde el endpoint admin
    /// de cleanup, disparado por cron externo.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTime now, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
