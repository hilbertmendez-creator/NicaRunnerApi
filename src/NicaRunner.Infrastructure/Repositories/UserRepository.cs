using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class UserRepository(NicaRunnerDbContext context) : IUserRepository
{
    // user-auth: "Email Address Normalization" (design.md §3.2) — mismo criterio
    // case-insensitive que GetByEmailOrUsernameAsync. Sin el LOWER(), un
    // "Hilbert@x.com" escrito con mayúsculas no encontraba la cuenta guardada como
    // "hilbert@x.com" y el forgot-password se iba en silencio (el flujo no revela
    // si el email existe), ni el login con Google podía enlazar la cuenta local.
    //
    // El índice único de Users.Email todavía es case-sensitive, así que pueden
    // convivir filas que solo difieren en mayúsculas (M3 las audita y avisa, no las
    // fusiona). Mientras eso siga siendo posible, ordenamos por Id para que la fila
    // elegida sea siempre la misma —la cuenta más vieja— y no la que la base
    // devuelva primero por casualidad.
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return context.Users
            .Where(u => u.Email.ToLower() == normalized)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(ct);
    }

    // user-auth: "Unified Identifier Login" (design.md §3.1) — una sola query
    // (`WHERE LOWER(Email) = @id OR Username = @id`), nunca dos lookups secuenciales
    // (un try-email-then-try-alias filtraría por timing cuál namespace matcheó).
    // Username ya llega normalizado en minúsculas desde el generador/UserManagementService,
    // así que solo Email necesita el LOWER() explícito acá.
    public Task<User?> GetByEmailOrUsernameAsync(string identifier, CancellationToken ct = default)
    {
        var normalized = identifier.Trim().ToLowerInvariant();
        return context.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == normalized || u.Username == normalized, ct);
    }

    public Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByResetTokenAsync(string token, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token, ct);

    // Comparación case-insensitive defensiva: el generador y el validador de alias ya
    // fuerzan minúsculas, pero un caller (p. ej. un admin editando el campo a mano) podría
    // enviar un valor mixto antes de que UserManagementService lo normalice.
    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
        context.Users.OrderBy(u => u.Email).ToListAsync(ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        context.Users.AnyAsync(u => u.Email == email, ct);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
        context.Users.AnyAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await context.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
