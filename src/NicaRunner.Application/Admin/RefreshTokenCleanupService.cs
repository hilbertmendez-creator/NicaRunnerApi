using NicaRunner.Application.Common.Interfaces;

namespace NicaRunner.Application.Admin;

public class RefreshTokenCleanupService(IRefreshTokenRepository repository) : IRefreshTokenCleanupService
{
    // 7 días de retención sobre revocados: suficiente para investigar replays
    // recientes (el interceptor del cliente descarta el token viejo en
    // segundos; una revocación de hace una semana ya no aporta señal a un
    // debug futuro).
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(7);

    public async Task<CleanupResult> RunAsync(CancellationToken ct = default)
    {
        var deleted = await repository.DeleteExpiredAsync(DateTime.UtcNow, RevokedRetention, ct);
        return new CleanupResult(deleted);
    }
}
