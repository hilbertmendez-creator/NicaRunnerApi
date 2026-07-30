using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common;

/// <summary>
/// Punto único de asignación de alias (Username), compartido por los tres sitios de
/// creación de usuario (design.md §3.4: alta manual por un admin, primer login de Google,
/// seed/bootstrap). Nunca implementa su propia lógica de composición — delega siempre en
/// <see cref="AliasGenerator"/> (pura) y resuelve unicidad contra <see cref="IUserRepository"/>.
/// </summary>
public class AliasAssigner(IUserRepository userRepository)
{
    // Sondeo de unicidad antes del insert (design.md §2.2, loop "k = 1..10").
    private const int MaxProbeAttempts = 10;

    // Reintentos de insert ante una colisión TOCTOU que el sondeo no pudo cerrar
    // (design.md §2.2, loop "insert attempt = 1..3").
    private const int MaxInsertRetries = 3;

    /// <summary>
    /// Sondea candidatos (intento 1..10) hasta encontrar un alias libre y lo asigna a
    /// <paramref name="user"/>.Username. No persiste — el caller decide cuándo agregar y
    /// guardar. Usado por los sitios de creación que no necesitan el reintento de insert
    /// (Google sign-in, seed/bootstrap): la ventana de colisión ahí es negligible y, si
    /// ocurre, el SaveChanges que ya hacía esa ruta simplemente falla de forma visible en
    /// vez de corromper datos.
    /// </summary>
    public async Task AssignAsync(User user, CancellationToken ct = default) =>
        user.Username = await ResolveUniqueCandidateAsync(user.Nombre, user.Email, ct);

    /// <summary>
    /// Asigna un alias, agrega el usuario y persiste, reintentando con un alias recién
    /// sondeado si el insert choca por una carrera con otra creación concurrente (TOCTOU
    /// que el sondeo previo no puede cerrar — design.md §2.2). Es el único camino
    /// interactivo (alta de usuario por un admin): agotar los reintentos se traduce en un
    /// 409 explícito en vez de dejar propagar un error sin manejar.
    /// </summary>
    public async Task AddWithUniqueAliasAsync(User user, CancellationToken ct = default)
    {
        await AssignAsync(user, ct);
        await userRepository.AddAsync(user, ct);

        for (var attempt = 1; attempt <= MaxInsertRetries; attempt++)
        {
            try
            {
                await userRepository.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException ex) when (IsUsernameConflict(ex))
            {
                if (attempt == MaxInsertRetries)
                    throw new ConflictException("No se pudo asignar un alias único.");

                await AssignAsync(user, ct);
            }
        }
    }

    private async Task<string> ResolveUniqueCandidateAsync(string nombre, string email, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxProbeAttempts; attempt++)
        {
            var candidate = AliasGenerator.Generate(nombre, email, attempt);
            if (!await userRepository.UsernameExistsAsync(candidate, ct))
                return candidate;
        }

        throw new ConflictException("No se pudo asignar un alias único.");
    }

    // Discrimina la violación del índice único de Username de cualquier otra
    // DbUpdateException (p. ej. Email) — un catch genérico enmascararía un error real
    // como si fuera una colisión de alias (design.md §2.2, nota bajo el diagrama).
    private static bool IsUsernameConflict(DbUpdateException ex) =>
        (ex.InnerException?.Message ?? ex.Message).Contains("Username", StringComparison.OrdinalIgnoreCase);
}
