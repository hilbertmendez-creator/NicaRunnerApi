using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Tests;

// design.md D11 + tasks.md 1.13: integración contra una base real (Sqlite) de que el
// índice único aditivo IX_Runners_RaceId_DorsalNormalizado —creado por la migración
// staged AddRegistrationsAndDorsalNormalization (add nullable -> backfill -> create
// index)— realmente rechaza la colisión numérica en la base, no solo en el servicio.
// Mismo harness que AliasIntegrationTests (Sqlite in-memory + Database.Migrate()).
public class DorsalNormalizationIntegrationTests
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

    private static async Task<(Race Race, Category Category)> SeedRaceAsync(NicaRunnerDbContext db, string suffix)
    {
        var admin = new User { Email = $"admin{suffix}@x.com", Nombre = $"Admin{suffix}", Role = UserRole.Administrador };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var category = new Category
        {
            Codigo = $"CAT{suffix}",
            NombreCategoria = $"Categoria {suffix}",
            Distancia = 5m,
            EdadMinima = 18,
            EdadMaxima = 99,
            Orden = 1
        };
        db.Categories.Add(category);

        var race = new Race
        {
            Nombre = $"Carrera {suffix}",
            FechaCarrera = DateTime.UtcNow.AddDays(30),
            JoinCode = Guid.NewGuid().ToString("N")[..8],
            AdminId = admin.Id
        };
        db.Races.Add(race);
        await db.SaveChangesAsync();

        db.RaceCategories.Add(new RaceCategory { RaceId = race.Id, CategoryId = category.Id });
        await db.SaveChangesAsync();

        return (race, category);
    }

    // Phase 2 (RunnerService) is the eventual caller of DorsalNormalizer on every write —
    // this test simulates that write path directly against the repository/DbContext,
    // which is all Phase 1 (domain + migration) can exercise on its own.
    private static Runner BuildRunner(int raceId, int categoryId, string dorsal, string nombre) => new()
    {
        RaceId = raceId,
        CategoryId = categoryId,
        Nombre = nombre,
        Dorsal = dorsal,
        DorsalNormalizado = DorsalNormalizer.Normalize(dorsal),
        Edad = 30
    };

    [Fact]
    public void Migrate_AplicaAddRegistrationsAndDorsalNormalization_SinErrores()
    {
        using var db = BuildMigratedDbContext();

        Assert.True(db.Database.CanConnect());
        Assert.Empty(db.Registrations.ToList());
        Assert.Empty(db.RegistrationLinks.ToList());
        Assert.Empty(db.ReservedDorsals.ToList());
    }

    [Fact]
    public async Task IndiceUnicoAditivo_RechazaDorsalConCerosALaIzquierdaEquivalente()
    {
        using var db = BuildMigratedDbContext();
        var (race, category) = await SeedRaceAsync(db, "A");

        db.Runners.Add(BuildRunner(race.Id, category.Id, "101", "Primero"));
        await db.SaveChangesAsync();

        db.Runners.Add(BuildRunner(race.Id, category.Id, "0101", "Segundo"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task IndiceUnicoAditivo_RechazaPrefijoAlfanumericoConPaddingEquivalente()
    {
        using var db = BuildMigratedDbContext();
        var (race, category) = await SeedRaceAsync(db, "B");

        db.Runners.Add(BuildRunner(race.Id, category.Id, "21K007", "Primero"));
        await db.SaveChangesAsync();

        db.Runners.Add(BuildRunner(race.Id, category.Id, "21K7", "Segundo"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    // El índice textual IX_Runners_RaceId_Dorsal preexistente sigue vigente — el nuevo
    // índice normalizado es aditivo, no un reemplazo.
    [Fact]
    public async Task IndiceTextualExistente_SigueRechazandoDorsalLiteralDuplicado()
    {
        using var db = BuildMigratedDbContext();
        var (race, category) = await SeedRaceAsync(db, "C");

        db.Runners.Add(BuildRunner(race.Id, category.Id, "202", "Primero"));
        await db.SaveChangesAsync();

        db.Runners.Add(BuildRunner(race.Id, category.Id, "202", "Segundo"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    // La unicidad normalizada comparte el mismo grano que la textual: por carrera, no
    // global — dos carreras distintas SÍ pueden tener dorsales que normalizan igual.
    [Fact]
    public async Task IndiceUnicoAditivo_EsPorCarrera_NoGlobal()
    {
        using var db = BuildMigratedDbContext();
        var (raceA, categoryA) = await SeedRaceAsync(db, "D1");
        var (raceB, categoryB) = await SeedRaceAsync(db, "D2");

        db.Runners.Add(BuildRunner(raceA.Id, categoryA.Id, "101", "CarreraA"));
        db.Runners.Add(BuildRunner(raceB.Id, categoryB.Id, "0101", "CarreraB"));

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Runners.CountAsync());
    }
}
