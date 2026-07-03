using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Security;

public class RefreshTokenService(
    IRefreshTokenRepository repository,
    IOptions<JwtSettings> options) : IRefreshTokenService
{
    // 256 bits de entropía. Base64url sin padding queda en 43 chars — pasa
    // limpio por URLs y headers HTTP sin escapar nada.
    private const int TokenByteLength = 32;
    private readonly JwtSettings _settings = options.Value;

    public async Task<IssuedRefreshToken> IssueAsync(User user, Guid? familyId = null, CancellationToken ct = default)
    {
        var raw = GenerateRawToken();
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(raw),
            FamilyId = familyId ?? Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshExpiryDays)
        };
        await repository.AddAsync(entity, ct);
        return new IssuedRefreshToken(raw, entity.ExpiresAt, entity.FamilyId);
    }

    public async Task<RotationResult> ValidateAndRotateAsync(string rawToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new InvalidCredentialsException("Refresh token requerido.");

        var hash = Hash(rawToken);
        var existing = await repository.GetByHashAsync(hash, ct);
        if (existing is null || existing.User is null || !existing.User.IsActive)
            throw new InvalidCredentialsException("Refresh token inválido.");

        // Replay detection: si llega un token ya revocado por rotación,
        // alguien tiene una copia robada. La rotación legítima nunca debería
        // reusar el viejo — el cliente honesto descarta el token apenas
        // recibe el siguiente par. Matamos la familia entera por las dudas.
        if (existing.RevokedAt is not null)
        {
            // Si ya estaba revocado por logout, esta llamada de RevokeFamilyAsync es
            // no-op (no quedan tokens activos). Si fue por rotación previa, esto
            // es replay genuino: matamos el resto de la cadena. En ambos casos
            // respondemos con el mismo mensaje neutro para no leakear al cliente
            // si la sesión fue cerrada por logout vs por replay.
            await repository.RevokeFamilyAsync(existing.FamilyId, RefreshTokenRevokedReason.ReplayDetected, ct);
            await repository.SaveChangesAsync(ct);
            throw new InvalidCredentialsException("Refresh token inválido.");
        }

        if (existing.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidCredentialsException("Refresh token inválido.");

        // Rotación: emite uno nuevo en la misma familia, marca el viejo como
        // rotated apuntando al nuevo (encadenamiento para auditar la cadena
        // de tokens si más adelante hace falta).
        var issued = await IssueAsync(existing.User, existing.FamilyId, ct);
        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedReason = RefreshTokenRevokedReason.Rotated;
        existing.ReplacedByTokenHash = Hash(issued.Token);
        await repository.SaveChangesAsync(ct);

        return new RotationResult(existing.User, issued);
    }

    public async Task LogoutAsync(string rawToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return;
        var existing = await repository.GetByHashAsync(Hash(rawToken), ct);
        if (existing is null)
            return;
        await repository.RevokeFamilyAsync(existing.FamilyId, RefreshTokenRevokedReason.Logout, ct);
        await repository.SaveChangesAsync(ct);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return Base64UrlEncode(bytes);
    }

    private static string Hash(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
