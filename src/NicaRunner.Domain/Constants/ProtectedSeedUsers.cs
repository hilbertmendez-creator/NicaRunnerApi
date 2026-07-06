namespace NicaRunner.Domain.Constants;

/// <summary>
/// Administradores semilla creados al desplegar; no pueden desactivarse ni cambiar de rol.
/// </summary>
public static class ProtectedSeedUsers
{
    public static readonly IReadOnlyList<string> Emails =
    [
        "hilbert.mendez@gmail.com",
        "evr86.skip@gmail.com",
        "edufisica@ymail.com"
    ];

    public static bool IsProtected(string email) =>
        Emails.Contains(email, StringComparer.OrdinalIgnoreCase);
}
