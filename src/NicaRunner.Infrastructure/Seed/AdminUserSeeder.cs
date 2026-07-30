using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Seed;

/// <summary>
/// Crea los administradores de backoffice iniciales si todavía no existen.
/// Seguro de re-ejecutar en cada deploy: solo agrega los que falten, nunca
/// sobreescribe un usuario ya existente (por si ya cambió su password/nombre).
/// </summary>
public static class AdminUserSeeder
{
    public static async Task SeedAsync(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        AliasAssigner aliasAssigner,
        string? defaultPassword,
        CancellationToken ct = default)
    {
        // Sin password configurada (Seed:DefaultAdminPassword) no hay nada seguro que
        // hashear — se omite el seed en vez de arrancar con una contraseña conocida.
        if (string.IsNullOrWhiteSpace(defaultPassword))
            return;

        var existingEmails = (await userRepository.GetAllAsync(ct))
            .Select(u => u.Email)
            .ToHashSet();

        foreach (var email in ProtectedSeedUsers.Emails)
        {
            if (existingEmails.Contains(email))
                continue;

            var user = new User
            {
                Email = email,
                Nombre = email,
                Role = UserRole.Administrador,
                Provider = AuthProvider.Local,
                PasswordHash = passwordHasher.Hash(defaultPassword),
                MustChangePassword = true,
                IsActive = true
            };

            // Sitio de creación 3 (design.md §3.4). Nombre = email no necesita manejo
            // especial: el paso 0 del normalizador (cortar en el primer '@') ya lo cubre
            // y produce, por ejemplo, "hilbert.mendez@gmail.com" -> "hilbertmendez".
            await aliasAssigner.AssignAsync(user, ct);
            await userRepository.AddAsync(user, ct);
        }

        await userRepository.SaveChangesAsync(ct);
    }
}
