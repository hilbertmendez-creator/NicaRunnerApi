using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Seed;

namespace NicaRunner.Tests;

// user-auth: "Email Address Normalization" — escenario "Case-differing duplicate
// detected during migration" (design.md §3.2, M3). Puramente detección: nunca fusiona.
public class EmailCaseDuplicateAuditServiceTests
{
    [Fact]
    public void FindCollisions_DosEmailsQueSoloDifierenEnMayusculas_DetectaLaColision()
    {
        var users = new List<User>
        {
            new() { Id = 1, Email = "a@x.com", Nombre = "A", Role = UserRole.Capturista },
            new() { Id = 2, Email = "A@x.com", Nombre = "B", Role = UserRole.Capturista }
        };

        var collisions = EmailCaseDuplicateAuditService.FindCollisions(users);

        var collision = Assert.Single(collisions);
        Assert.Equal("a@x.com", collision.NormalizedEmail);
        Assert.Equal(2, collision.Users.Count);
        Assert.Contains(collision.Users, u => u is { UserId: 1, Email: "a@x.com" });
        Assert.Contains(collision.Users, u => u is { UserId: 2, Email: "A@x.com" });
    }

    [Fact]
    public void FindCollisions_SinColisiones_DevuelveListaVacia()
    {
        var users = new List<User>
        {
            new() { Id = 1, Email = "a@x.com", Nombre = "A", Role = UserRole.Capturista },
            new() { Id = 2, Email = "b@x.com", Nombre = "B", Role = UserRole.Capturista }
        };

        Assert.Empty(EmailCaseDuplicateAuditService.FindCollisions(users));
    }

    // Nunca "fusiona": la función solo reporta, nunca modifica ni deduplica la
    // colección de entrada.
    [Fact]
    public void FindCollisions_NuncaModificaLosUsuariosOriginales()
    {
        var users = new List<User>
        {
            new() { Id = 1, Email = "a@x.com", Nombre = "A", Role = UserRole.Capturista },
            new() { Id = 2, Email = "A@x.com", Nombre = "B", Role = UserRole.Capturista }
        };

        EmailCaseDuplicateAuditService.FindCollisions(users);

        Assert.Equal("a@x.com", users[0].Email);
        Assert.Equal("A@x.com", users[1].Email);
    }
}
