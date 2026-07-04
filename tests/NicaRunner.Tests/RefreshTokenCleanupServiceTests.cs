using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Admin;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;

namespace NicaRunner.Tests;

// Test de integración liviana sobre Sqlite in-memory: el service delega en
// EF.ExecuteDeleteAsync (SQL puro), así que un mock del repo no probaría lo
// que importa — que el WHERE compuesto realmente encuentra los tokens
// correctos. La base es una Sqlite en memoria abierta con SharedCache para
// que el ctor y las llamadas compartan la misma conexión.
public class RefreshTokenCleanupServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly NicaRunnerDbContext _db;

    public RefreshTokenCleanupServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NicaRunnerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new NicaRunnerDbContext(options);
        _db.Database.EnsureCreated();

        // Seed de un usuario base para poder crear tokens con FK válido.
        _db.Users.Add(new User { Id = 1, Email = "a@b.com", Nombre = "A", Role = UserRole.Administrador });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private RefreshTokenCleanupService BuildService() =>
        new(new RefreshTokenRepository(_db));

    private static RefreshToken NewToken(
        string tokenHash,
        DateTime expiresAt,
        DateTime? revokedAt = null,
        RefreshTokenRevokedReason? revokedReason = null) => new()
    {
        UserId = 1,
        TokenHash = tokenHash,
        FamilyId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow.AddDays(-30),
        ExpiresAt = expiresAt,
        RevokedAt = revokedAt,
        RevokedReason = revokedReason,
    };

    // Un token que NO expiró y NO fue revocado NO se toca.
    [Fact]
    public async Task Cleanup_TokenActivo_NoSeBorra()
    {
        _db.RefreshTokens.Add(NewToken("active", DateTime.UtcNow.AddDays(20)));
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, await _db.RefreshTokens.CountAsync());
    }

    // Un token con ExpiresAt en el pasado se borra sin importar RevokedAt.
    [Fact]
    public async Task Cleanup_TokenExpiradoPorTTL_SeBorra()
    {
        _db.RefreshTokens.Add(NewToken("expired", DateTime.UtcNow.AddMinutes(-1)));
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(1, result.Deleted);
        Assert.Equal(0, await _db.RefreshTokens.CountAsync());
    }

    // Un token revocado hace poco (dentro de la ventana de retención de 7
    // días) NO se borra — sirve para auditar replays recientes.
    [Fact]
    public async Task Cleanup_TokenRevocadoRecientemente_NoSeBorra()
    {
        _db.RefreshTokens.Add(NewToken(
            "recently-revoked",
            expiresAt: DateTime.UtcNow.AddDays(20),
            revokedAt: DateTime.UtcNow.AddDays(-2),
            revokedReason: RefreshTokenRevokedReason.Rotated));
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(0, result.Deleted);
    }

    // Un token revocado hace más que la ventana se borra aunque no haya
    // expirado por TTL. Ejemplo típico: logout de hace 3 semanas.
    [Fact]
    public async Task Cleanup_TokenRevocadoHaceMas7Dias_SeBorra()
    {
        _db.RefreshTokens.Add(NewToken(
            "long-ago-revoked",
            expiresAt: DateTime.UtcNow.AddDays(20),
            revokedAt: DateTime.UtcNow.AddDays(-21),
            revokedReason: RefreshTokenRevokedReason.Logout));
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(1, result.Deleted);
        Assert.Equal(0, await _db.RefreshTokens.CountAsync());
    }

    // Corrida realista con mezcla: activos, expirados, revocados recientes,
    // revocados viejos. Solo los expirados y los revocados-viejos se van.
    [Fact]
    public async Task Cleanup_Mezcla_BorraSoloLosCandidatos()
    {
        _db.RefreshTokens.AddRange(
            NewToken("a1", DateTime.UtcNow.AddDays(20)),                                        // activo
            NewToken("a2", DateTime.UtcNow.AddDays(5)),                                         // activo
            NewToken("e1", DateTime.UtcNow.AddMinutes(-10)),                                    // expirado
            NewToken("e2", DateTime.UtcNow.AddDays(-3)),                                        // expirado
            NewToken("r1", DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(-1),
                RefreshTokenRevokedReason.Rotated),                                             // revoc reciente
            NewToken("r2", DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(-30),
                RefreshTokenRevokedReason.Logout));                                             // revoc viejo
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(3, result.Deleted); // e1 + e2 + r2
        var restantes = await _db.RefreshTokens.Select(t => t.TokenHash).OrderBy(h => h).ToListAsync();
        Assert.Equal(new[] { "a1", "a2", "r1" }, restantes);
    }
}
