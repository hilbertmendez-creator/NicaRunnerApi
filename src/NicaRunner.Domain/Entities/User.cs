namespace NicaRunner.Domain.Entities;

public enum UserRole
{
    Capturista,
    Administrador,
    Lector
}

public enum AuthProvider
{
    Local,
    Google,
    LocalAndGoogle
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public AuthProvider Provider { get; set; } = AuthProvider.Local;
    public string Nombre { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Alias/identificador secundario para login unificado (user-alias). Nullable hasta
    // que M4 (PR2) lo vuelva NOT NULL una vez completado el backfill (M2, esta PR).
    public string? Username { get; set; }

    // Lockout (login-lockout, mecánica completa en PR2). Se agregan en esta migración
    // porque comparten M1 con Username, pero AuthService.LoginAsync todavía no las lee
    // ni las escribe hasta PR2.
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime? LastFailedLoginUtc { get; set; }
}
