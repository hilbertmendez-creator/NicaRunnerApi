using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

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

    public static async Task SeedAsync(NicaRunnerDbContext db, IPasswordHasher passwordHasher, string? defaultPassword, CancellationToken ct = default)
    {
        // Sin password configurada (Seed:DefaultAdminPassword) no hay nada seguro que
        // hashear — se omite el seed en vez de arrancar con una contraseña conocida.
        if (string.IsNullOrWhiteSpace(defaultPassword))
            return;

        foreach (var email in AdminEmails)
        {
            var exists = await db.Users.AnyAsync(u => u.Email == email, ct);
            if (exists)
                continue;

            db.Users.Add(new User
            {
                Email = email,
                Nombre = email,
                Role = UserRole.Administrador,
                Provider = AuthProvider.Local,
                PasswordHash = passwordHasher.Hash(defaultPassword),
                MustChangePassword = true,
                IsActive = true
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
