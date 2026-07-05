namespace NicaRunner.Domain.Entities;

public enum RefreshTokenRevokedReason
{
    Rotated,
    Logout,
    ReplayDetected,
    ManualRevoke
}

/// <summary>
/// Token de refresco opaco (no JWT) que permite renovar el access token sin
/// forzar al usuario a re-loguearse. Cada par de tokens (access + refresh)
/// emitidos por el mismo login pertenecen a la misma <c>FamilyId</c>; cuando
/// se rota un token, el viejo queda revocado con <c>ReplacedByTokenHash</c>
/// apuntando al nuevo. Si alguna vez llega un token ya revocado por rotación
/// se considera replay y se revoca la familia entera.
///
/// El valor crudo del token NUNCA se persiste; solo <c>TokenHash</c>
/// (SHA-256 hex) — así una filtración de la BD no expone tokens utilizables.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public RefreshTokenRevokedReason? RevokedReason { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
