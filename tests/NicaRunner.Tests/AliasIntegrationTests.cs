using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;

namespace NicaRunner.Tests;

// Integración (Sqlite in-memory) para el alcance que un mock no puede cubrir: que el
// índice único filtrado de Username realmente rechaza duplicados en la base, y que
// AddUserUsername (M1) + FixPostgresLockoutColumns aplican limpio con Database.Migrate()
// -- las demás migraciones del historial ya se ejecutan siempre en esta suite vía
// EnsureCreated, pero EnsureCreated nunca corre el SQL de Up() de una migración: solo
// Migrate() ejecuta el código que se hand-authored en este PR.
public class AliasIntegrationTests
{
    private static NicaRunnerDbContext BuildMigratedDbContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<NicaRunnerDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new NicaRunnerDbContext(options);
        db.Database.Migrate();
        return db;
    }

    [Fact]
    public void Migrate_AplicaTodoElHistorialIncluidaAddUserUsername_SinErrores()
    {
        using var db = BuildMigratedDbContext();

        Assert.True(db.Database.CanConnect());
        Assert.Empty(db.Users.Where(u => u.Username == "no-existe").ToList());
    }

    [Fact]
    public async Task IndiceUnicoFiltrado_RechazaAliasDuplicado()
    {
        using var db = BuildMigratedDbContext();
        db.Users.Add(new User { Email = "a@x.com", Nombre = "A", Username = "hmendezv", Role = UserRole.Capturista });
        await db.SaveChangesAsync();

        db.Users.Add(new User { Email = "b@x.com", Nombre = "B", Username = "hmendezv", Role = UserRole.Capturista });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    // El índice es filtrado ("Username" IS NOT NULL): varias filas con Username nulo
    // (usuarios pre-backfill) deben poder coexistir sin chocar entre sí.
    [Fact]
    public async Task IndiceUnicoFiltrado_PermiteMultiplesUsuariosConUsernameNulo()
    {
        using var db = BuildMigratedDbContext();
        db.Users.Add(new User { Email = "a@x.com", Nombre = "A", Role = UserRole.Capturista });
        db.Users.Add(new User { Email = "b@x.com", Nombre = "B", Role = UserRole.Capturista });

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Users.CountAsync());
    }

    // TOCTOU (user-alias: "Constraint violation on the final insert") probado contra una
    // base real, no un mock: el sondeo de AliasAssigner sí ve el alias ya persistido por
    // la primera llamada y el segundo usuario recibe el sufijo de colisión.
    [Fact]
    public async Task AliasAssigner_ContraBaseReal_ElSegundoUsuarioConElMismoNombreRecibeSufijo()
    {
        using var db = BuildMigratedDbContext();
        var users = new UserRepository(db);
        var assigner = new AliasAssigner(users);

        var primero = new User { Email = "primero@x.com", Nombre = "Hilbert Mendez Velasquez", Role = UserRole.Capturista };
        await assigner.AddWithUniqueAliasAsync(primero);

        var segundo = new User { Email = "segundo@x.com", Nombre = "Hilbert Mendez Velasquez", Role = UserRole.Capturista };
        await assigner.AddWithUniqueAliasAsync(segundo);

        Assert.Equal("hmendezv", primero.Username);
        Assert.Equal("hmendezv2", segundo.Username);
    }

    // user-auth: "Email Address Normalization" — contra base real porque el punto
    // es justamente cómo compara el motor, que un mock no puede reproducir.
    [Theory]
    [InlineData("hilbert@x.com")]
    [InlineData("Hilbert@X.com")]
    [InlineData("HILBERT@X.COM")]
    [InlineData("  Hilbert@x.com  ")]
    public async Task GetByEmailAsync_IgnoraMayusculasYEspacios(string consulta)
    {
        using var db = BuildMigratedDbContext();
        db.Users.Add(new User { Email = "hilbert@x.com", Nombre = "H", Role = UserRole.Capturista });
        await db.SaveChangesAsync();

        var encontrado = await new UserRepository(db).GetByEmailAsync(consulta);

        Assert.NotNull(encontrado);
        Assert.Equal("hilbert@x.com", encontrado!.Email);
    }

    // Mientras el índice único de Email siga siendo case-sensitive pueden convivir
    // filas que solo difieren en mayúsculas. La búsqueda tiene que devolver siempre
    // la misma —la más vieja— y no la que la base saque primero por casualidad.
    [Fact]
    public async Task GetByEmailAsync_ConColisionPorMayusculas_DevuelveLaCuentaMasVieja()
    {
        using var db = BuildMigratedDbContext();
        db.Users.Add(new User { Email = "hilbert@x.com", Nombre = "Vieja", Role = UserRole.Capturista });
        db.Users.Add(new User { Email = "HILBERT@x.com", Nombre = "Nueva", Role = UserRole.Capturista });
        await db.SaveChangesAsync();

        var users = new UserRepository(db);

        Assert.Equal("Vieja", (await users.GetByEmailAsync("hilbert@x.com"))!.Nombre);
        Assert.Equal("Vieja", (await users.GetByEmailAsync("HILBERT@x.com"))!.Nombre);
    }

    [Fact]
    public async Task GetByEmailAsync_EmailInexistente_DevuelveNull()
    {
        using var db = BuildMigratedDbContext();
        db.Users.Add(new User { Email = "hilbert@x.com", Nombre = "H", Role = UserRole.Capturista });
        await db.SaveChangesAsync();

        Assert.Null(await new UserRepository(db).GetByEmailAsync("otro@x.com"));
    }
}
