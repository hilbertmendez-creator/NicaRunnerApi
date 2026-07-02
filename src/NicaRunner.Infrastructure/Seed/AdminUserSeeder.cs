using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Seed;

/// <summary>
/// Crea los administradores de backoffice iniciales si todavía no existen.
/// Seguro de re-ejecutar en cada deploy: solo agrega los que falten, nunca
/// sobreescribe un usuario ya existente (por si ya cambió su password/nombre).
/// </summary>
public static class AdminUserSeeder
{
    private static readonly string[] AdminEmails =
    [
        "hilbert.mendez@gmail.com",
        "evr86.skip@gmail.com",
        "edufisica@ymail.com"
    ];

    public static async Task SeedAsync(IUserRepository userRepository, IPasswordHasher passwordHasher, string? defaultPassword, CancellationToken ct = default)
    {
        // Sin password configurada (Seed:DefaultAdminPassword) no hay nada seguro que
        // hashear — se omite el seed en vez de arrancar con una contraseña conocida.
        if (string.IsNullOrWhiteSpace(defaultPassword))
            return;

        var existingEmails = (await userRepository.GetAllAsync(ct))
            .Select(u => u.Email)
            .ToHashSet();

        foreach (var email in AdminEmails)
        {
            if (existingEmails.Contains(email))
                continue;

            await userRepository.AddAsync(new User
            {
                Email = email,
                Nombre = email,
                Role = UserRole.Administrador,
                Provider = AuthProvider.Local,
                PasswordHash = passwordHasher.Hash(defaultPassword),
                MustChangePassword = true,
                IsActive = true
            }, ct);
        }

        await userRepository.SaveChangesAsync(ct);
    }
}
