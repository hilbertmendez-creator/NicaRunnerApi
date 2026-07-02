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
}
