using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;

namespace NicaRunner.Tests;

// backoffice-user-status-toggle: "In-flight Capturista deactivation warning" (design.md D6).
// Load-bearing: RaceService.JoinByCodeAsync (RaceService.cs:152-153) returns early when
// race.AdminId == userId, so the race's own admin NEVER gets a RaceJudge row. A judges-only
// query would silently miss a running race operated by its own admin — the query must be the
// union (AdminId == userId) OR (Judges.Any(UserId == userId)). Contra Sqlite real, no un mock.
public class RaceRepositoryActiveForUserTests
{
    [Fact]
    public async Task GetActiveForUserAsync_IncluyeAdminOJuez_ExcluyePlaneadaYTerminada()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new NicaRunnerDbContext(new DbContextOptionsBuilder<NicaRunnerDbContext>().UseSqlite(connection).Options);
        db.Database.Migrate();

        var admin = new User { Email = "admin@x.com", Nombre = "Admin", Role = UserRole.Administrador };
        var otroAdmin = new User { Email = "otro@x.com", Nombre = "Otro", Role = UserRole.Administrador };
        var capturista = new User { Email = "cap@x.com", Nombre = "Cap", Role = UserRole.Capturista };
        db.Users.AddRange(admin, otroAdmin, capturista);
        await db.SaveChangesAsync();

        // EnCurso, operada por su propio admin, SIN fila RaceJudge -> debe aparecer para
        // `admin` vía el lado AdminId (JoinByCodeAsync nunca crea esa fila para el admin).
        var porSuAdmin = new Race { Nombre = "Carrera del Admin", FechaCarrera = DateTime.UtcNow, JoinCode = "AAA111", AdminId = admin.Id, Estado = RaceStatus.EnCurso };
        // EnCurso, `capturista` es juez -> debe aparecer para `capturista` vía Judges.Any.
        var comoJuez = new Race { Nombre = "Carrera del Juez", FechaCarrera = DateTime.UtcNow, JoinCode = "BBB222", AdminId = otroAdmin.Id, Estado = RaceStatus.EnCurso };
        // Planeada, operada por `admin` -> nunca debe aparecer.
        var planeada = new Race { Nombre = "Carrera Planeada", FechaCarrera = DateTime.UtcNow, JoinCode = "CCC333", AdminId = admin.Id, Estado = RaceStatus.Planeada };
        // Terminada, `capturista` es juez -> nunca debe aparecer.
        var terminada = new Race { Nombre = "Carrera Terminada", FechaCarrera = DateTime.UtcNow, JoinCode = "DDD444", AdminId = otroAdmin.Id, Estado = RaceStatus.Terminada };
        db.Races.AddRange(porSuAdmin, comoJuez, planeada, terminada);
        await db.SaveChangesAsync();

        db.RaceJudges.Add(new RaceJudge { RaceId = comoJuez.Id, UserId = capturista.Id });
        db.RaceJudges.Add(new RaceJudge { RaceId = terminada.Id, UserId = capturista.Id });
        await db.SaveChangesAsync();

        var repository = new RaceRepository(db);
        var adminResults = await repository.GetActiveForUserAsync(admin.Id);
        var juezResults = await repository.GetActiveForUserAsync(capturista.Id);

        var adminRace = Assert.Single(adminResults);
        Assert.Equal(porSuAdmin.Id, adminRace.Id);
        Assert.Equal("Carrera del Admin", adminRace.Nombre);

        var juezRace = Assert.Single(juezResults);
        Assert.Equal(comoJuez.Id, juezRace.Id);
        Assert.Equal("Carrera del Juez", juezRace.Nombre);
    }
}
