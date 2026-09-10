using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;

namespace NicaRunner.Infrastructure.Security;

// backoffice-user-status-toggle PR3 (design.md D2) -- IMemoryCache por usuario, TTL
// absoluto (no sliding) de AccountStatusOptions.CacheSeconds. Advertencia de
// multi-instancia (honesta, no asumida): IMemoryCache es por-proceso -- con N instancias
// invalidar en A no toca B, revocación acotada a 30s/instancia si N > 1. Hoy N=1 (Render
// free plan, sin ConnectionStrings:Redis -- Program.cs:107-117); un reemplazo Redis-backed
// usaría esta misma interfaz, no se construye acá por falta de Redis contra qué probarlo.
public class AccountStatusCache(
    IMemoryCache cache,
    IUserRepository userRepository,
    IOptions<AccountStatusOptions> options) : IAccountStatusCache
{
    private static string CacheKey(int userId) => $"account-status:{userId}";

    public async Task<bool> IsActiveAsync(int userId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey(userId), out bool cached))
            return cached;

        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {userId}.");

        cache.Set(CacheKey(userId), user.IsActive, TimeSpan.FromSeconds(options.Value.CacheSeconds));
        return user.IsActive;
    }

    public void Invalidate(int userId) => cache.Remove(CacheKey(userId));
}
