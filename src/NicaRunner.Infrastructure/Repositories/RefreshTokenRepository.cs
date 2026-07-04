using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class RefreshTokenRepository(NicaRunnerDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public async Task<int> RevokeFamilyAsync(Guid familyId, RefreshTokenRevokedReason reason, CancellationToken ct = default)
    {
        // ExecuteUpdate envía un UPDATE en SQL plano sin cargar las filas en memoria.
        // Importante para el caso "replay detected" donde la familia puede tener
        // decenas de tokens rotados y no queremos materializarlos todos.
        var now = DateTime.UtcNow;
        return await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, reason), ct);
    }

    public async Task<int> DeleteExpiredAsync(DateTime now, TimeSpan revokedRetention, CancellationToken ct = default)
    {
        // ExecuteDeleteAsync: SQL DELETE plano, no materializa filas. Ideal
        // para un cron que puede encontrarse decenas de miles de tokens viejos.
        // Criterio: token ya expiró por TTL, O fue revocado hace más que la
        // ventana de retención (por defecto 7 días — sirve para auditar
        // replays recientes; pasado ese tiempo la cadena no importa).
        var revokedCutoff = now - revokedRetention;
        return await db.RefreshTokens
            .Where(t => t.ExpiresAt < now
                     || (t.RevokedAt != null && t.RevokedAt < revokedCutoff))
            .ExecuteDeleteAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
