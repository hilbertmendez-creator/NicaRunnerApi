using Microsoft.EntityFrameworkCore;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;
using NicaRunner.Infrastructure.Seed;

namespace NicaRunner.Tests;

// design.md Decisión 2: backfill idempotente, código de aplicación (no SQL) — el
// generador es CSPRNG en C#, no una expresión portable entre Sqlite/Postgres. Contra
// Sqlite in-memory real (mismo patrón que AliasIntegrationTests): el punto es que el
// filtro WHERE "PublicShareKey" IS NULL de la query y el índice único filtrado
// coexisten sin chocar, algo que un mock de IRunnerRepository no puede demostrar.
public class RunnerShareKeyBackfillServiceTests
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

    private static async Task<(Race Race, Category Category)> SeedRaceAndCategoryAsync(NicaRunnerDbContext db)
    {
        var admin = new User { Email = "admin@x.com", Nombre = "Admin", Role = UserRole.Administrador };
        db.Users.Add(admin);
        var category = new Category { Codigo = "GEN", NombreCategoria = "General", Distancia = 5 };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var race = new Race { Nombre = "Carrera", FechaCarrera = DateTime.UtcNow, JoinCode = "JC1", AdminId = admin.Id };
        db.Races.Add(race);
        await db.SaveChangesAsync();

        return (race, category);
    }

    [Fact]
    public async Task BackfillAsync_AsignaClaveSoloALosCorredoresSinClave_YDejaIntactosLosDemas()
    {
        using var db = BuildMigratedDbContext();
        var (race, category) = await SeedRaceAndCategoryAsync(db);

        var conClave = new Runner
        {
            RaceId = race.Id, CategoryId = category.Id, Nombre = "Con clave", Dorsal = "1",
            PublicShareKey = "clave-existente-ya-asignada"
        };
        var sinClave = new Runner { RaceId = race.Id, CategoryId = category.Id, Nombre = "Sin clave", Dorsal = "2" };
        db.Runners.AddRange(conClave, sinClave);
        await db.SaveChangesAsync();

        await RunnerShareKeyBackfillService.BackfillAsync(new RunnerRepository(db));

        Assert.Equal("clave-existente-ya-asignada", conClave.PublicShareKey);
        Assert.NotNull(sinClave.PublicShareKey);
        Assert.Equal(22, sinClave.PublicShareKey!.Length);
    }

    [Fact]
    public async Task BackfillAsync_EsIdempotente_SegundaCorridaNoReasignaClaves()
    {
        using var db = BuildMigratedDbContext();
        var (race, category) = await SeedRaceAndCategoryAsync(db);

        var runner = new Runner { RaceId = race.Id, CategoryId = category.Id, Nombre = "Runner", Dorsal = "1" };
        db.Runners.Add(runner);
        await db.SaveChangesAsync();

        var repository = new RunnerRepository(db);
        await RunnerShareKeyBackfillService.BackfillAsync(repository);
        var primeraClave = runner.PublicShareKey;

        await RunnerShareKeyBackfillService.BackfillAsync(repository);

        Assert.Equal(primeraClave, runner.PublicShareKey);
    }

    [Fact]
    public async Task BackfillAsync_AsignaClavesUnicasAVariosCorredoresSinClave()
    {
        using var db = BuildMigratedDbContext();
        var (race, category) = await SeedRaceAndCategoryAsync(db);

        var runners = Enumerable.Range(1, 5)
            .Select(i => new Runner { RaceId = race.Id, CategoryId = category.Id, Nombre = $"R{i}", Dorsal = i.ToString() })
            .ToList();
        db.Runners.AddRange(runners);
        await db.SaveChangesAsync();

        await RunnerShareKeyBackfillService.BackfillAsync(new RunnerRepository(db));

        var claves = runners.Select(r => r.PublicShareKey).ToList();
        Assert.All(claves, Assert.NotNull);
        Assert.Equal(claves.Count, claves.Distinct().Count());
    }
}
