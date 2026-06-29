using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public record IssuedRefreshToken(string Token, DateTime ExpiresAtUtc, Guid FamilyId);

public record RotationResult(User User, IssuedRefreshToken NewToken);

public interface IRefreshTokenService
{
    /// <summary>
    /// Crea un token de refresco nuevo y lo persiste hasheado. Si se pasa
    /// <paramref name="familyId"/>, el token nuevo pertenece a esa familia
    /// (caso rotación); si no, abre una familia nueva (caso login).
    /// El valor crudo del token solo se devuelve aquí — no se puede recuperar
    /// después porque solo guardamos el hash.
    /// </summary>
    Task<IssuedRefreshToken> IssueAsync(User user, Guid? familyId = null, CancellationToken ct = default);

    /// <summary>
    /// Valida el token plano que envía el cliente, lo rota (revoca el viejo,
    /// emite uno nuevo en la misma familia) y devuelve el usuario para que
    /// el caller genere un access token. Lanza
    /// <c>InvalidCredentialsException</c> si el token no existe, está
    /// expirado, o si detecta replay (en ese caso además revoca la familia
    /// entera).
    /// </summary>
    Task<RotationResult> ValidateAndRotateAsync(string rawToken, CancellationToken ct = default);

    /// <summary>
    /// Revoca la familia del token recibido (típicamente desde logout). No
    /// falla si el token ya está revocado o no existe — un logout sobre un
    /// token inválido es no-op idempotente.
    /// </summary>
    Task LogoutAsync(string rawToken, CancellationToken ct = default);
}
