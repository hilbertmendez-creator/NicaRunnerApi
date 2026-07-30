using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Seed;

/// <summary>
/// M3 (design.md §3.2, "Migration / Rollout"): audita, antes de cualquier migración
/// futura que normalice <c>Email</c> a minúsculas, las cuentas cuyo email colisiona
/// solo por mayúsculas/minúsculas (p. ej. <c>a@x.com</c> y <c>A@x.com</c>) — hoy
/// conviven porque el índice único de <c>Users.Email</c> es case-sensitive.
/// Puramente detección: design.md exige "fail loudly, never merge" (user-auth:
/// "Email Address Normalization", escenario "Case-differing duplicate detected
/// during migration") — esta clase NUNCA fusiona ni modifica filas, solo reporta
/// los grupos en colisión para que un humano los resuelva a mano. M4 (Username NOT
/// NULL) queda bloqueado mientras existan colisiones sin resolver (design.md,
/// Migration/Rollout: "M3 blocks M4 on any case-collision").
/// </summary>
public static class EmailCaseDuplicateAuditService
{
    public static async Task<IReadOnlyList<EmailCaseCollision>> FindCollisionsAsync(
        IUserRepository userRepository, CancellationToken ct = default)
    {
        var users = await userRepository.GetAllAsync(ct);
        return FindCollisions(users);
    }

    // Función pura sobre la lista ya cargada, separada de FindCollisionsAsync para
    // poder testearla sin repositorio ni base de datos (mismo estilo que
    // AliasGenerator: sin I/O, determinista).
    public static IReadOnlyList<EmailCaseCollision> FindCollisions(IEnumerable<User> users) =>
        users
            .GroupBy(u => u.Email.ToLowerInvariant())
            .Where(g => g.Select(u => u.Email).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(g => new EmailCaseCollision(g.Key, g.Select(u => new EmailCaseCollisionMember(u.Id, u.Email)).ToList()))
            .ToList();
}

/// <summary>Un grupo de 2+ usuarios cuyos emails solo difieren en mayúsculas/minúsculas.</summary>
public record EmailCaseCollision(string NormalizedEmail, IReadOnlyList<EmailCaseCollisionMember> Users);

public record EmailCaseCollisionMember(int UserId, string Email);
