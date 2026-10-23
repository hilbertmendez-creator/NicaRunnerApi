using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;

namespace NicaRunner.Tests;

// backoffice-user-status-toggle: "Only active admins count" (design.md D5). Contra Sqlite
// real, no un mock, porque el defecto viviría en el WHERE de la consulta.
public class UserRepositoryTests
{
    [Fact]
    public async Task CountActiveByRoleAsync_CuentaSoloAdministradoresActivos()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        // EF Core 9+: las migraciones son de Postgres y el modelo que EF arma para Sqlite
        // nunca coincide con su snapshot, así que Migrate() lo trataría como error. Acá
        // se prueba que apliquen, no que coincidan.
        using var db = new NicaRunnerDbContext(new DbContextOptionsBuilder<NicaRunnerDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);
        db.Database.Migrate();
        db.Users.AddRange(
            new User { Email = "a1@x.com", Nombre = "A1", Role = UserRole.Administrador, IsActive = true },
            new User { Email = "a2@x.com", Nombre = "A2", Role = UserRole.Administrador, IsActive = true },
            new User { Email = "a3@x.com", Nombre = "A3", Role = UserRole.Administrador, IsActive = false },
            new User { Email = "c1@x.com", Nombre = "C1", Role = UserRole.Capturista, IsActive = true });
        await db.SaveChangesAsync();

        var count = await new UserRepository(db).CountActiveByRoleAsync(UserRole.Administrador);

        Assert.Equal(2, count);
    }
}
