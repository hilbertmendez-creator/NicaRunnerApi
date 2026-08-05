using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Registrations;
using NicaRunner.Application.Runners;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Excel;
using NicaRunner.Infrastructure.Repositories;

namespace NicaRunner.Tests;

// tasks.md 3.6, design.md D12/D13: integración contra Sqlite real (mismo harness que
// RegistrationServiceIntegrationTests) para el alcance que un mock no cubre — el template
// round-trip real vía ExcelRegistrationParser/ClosedXML, y el agotamiento de cupo a mitad
// de lote aplicando el conditional UPDATE atómico sobre datos persistidos. Vive en su
// propio archivo (no en RegistrationServiceIntegrationTests.cs) para no pasar el límite de
// 500 líneas por archivo del proyecto.
public class RegistrationBulkConfirmIntegrationTests
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

    private static async Task<(Race Race, RaceCategory RaceCategory, User Admin)> SeedRaceAsync(
        NicaRunnerDbContext db, string suffix, int? capacidad)
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
            AdminId = admin.Id,
            InscripcionesAbiertas = true
        };
        db.Races.Add(race);
        await db.SaveChangesAsync();

        var raceCategory = new RaceCategory { RaceId = race.Id, CategoryId = category.Id, Capacidad = capacidad, Precio = 100m };
        db.RaceCategories.Add(raceCategory);
        await db.SaveChangesAsync();

        return (race, raceCategory, admin);
    }

    private static async Task<RaceCategory> AddRaceCategoryAsync(NicaRunnerDbContext db, Race race, string suffix, int? capacidad)
    {
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
        await db.SaveChangesAsync();

        var raceCategory = new RaceCategory { RaceId = race.Id, CategoryId = category.Id, Capacidad = capacidad, Precio = 100m };
        db.RaceCategories.Add(raceCategory);
        await db.SaveChangesAsync();

        return raceCategory;
    }

    private static Registration ComprobanteSubidoRegistration(int raceId, int raceCategoryId, string nombre) => new()
    {
        RaceId = raceId,
        RaceCategoryId = raceCategoryId,
        Nombre = nombre,
        FechaNacimiento = new DateTime(1990, 1, 1),
        Estado = RegistrationStatus.ComprobanteSubido,
        ReferenciaTransferencia = "REF-1"
    };

    private static RunnerService BuildRunnerService(NicaRunnerDbContext db) => new(
        new RunnerRepository(db),
        new RaceRepository(db),
        new RaceCategoryRepository(db),
        new Mock<IExcelRunnerParser>().Object,
        new ReservedDorsalRepository(db));

    private static RegistrationService BuildRegistrationService(NicaRunnerDbContext db, RunnerService runnerService) => new(
        new RegistrationRepository(db),
        new RegistrationLinkRepository(db),
        new RaceRepository(db),
        new RaceCategoryRepository(db),
        runnerService,
        new AuditService(new AuditLogRepository(db)),
        Options.Create(new RegistrationOptions()),
        new ExcelRegistrationParser());

    // registration-review spec.md "Template download scoped to pending registrations" +
    // full round trip: download -> fill Dorsal -> upload -> Runners created.
    [Fact]
    public async Task ConfirmBulkAsync_PlantillaRoundTrip_CreaRunnersConDorsalDelArchivo()
    {
        using var db = BuildMigratedDbContext();
        var (race, raceCategory, admin) = await SeedRaceAsync(db, "BULK1", capacidad: 10);

        var reg1 = ComprobanteSubidoRegistration(race.Id, raceCategory.Id, "Corredor Uno");
        var reg2 = ComprobanteSubidoRegistration(race.Id, raceCategory.Id, "Corredor Dos");
        db.Registrations.AddRange(reg1, reg2);
        await db.SaveChangesAsync();
        var reg1Id = reg1.Id;
        db.ChangeTracker.Clear();

        var runnerService = BuildRunnerService(db);
        var registrationService = BuildRegistrationService(db, runnerService);

        var templateBytes = await registrationService.GenerateBulkConfirmTemplateAsync(race.Id, CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(templateBytes));
        var sheet = workbook.Worksheets.First();
        Assert.Equal(3, sheet.LastRowUsed()!.RowNumber()); // encabezado + 2 filas
        Assert.True(workbook.Worksheets.TryGetWorksheet("Categorías (referencia)", out var referenceSheet));
        Assert.Equal("Categoria BULK1", referenceSheet!.Cell(2, 2).GetString());

        for (var row = 2; row <= 3; row++)
        {
            var id = sheet.Cell(row, 1).GetValue<int>();
            sheet.Cell(row, 7).Value = id == reg1Id ? "701" : "702";
        }

        using var uploadStream = new MemoryStream();
        workbook.SaveAs(uploadStream);
        uploadStream.Position = 0;

        var result = await registrationService.ConfirmBulkAsync(race.Id, uploadStream, admin.Id, CancellationToken.None);

        Assert.Equal(2, result.TotalFilas);
        Assert.Equal(2, result.Confirmadas);
        Assert.Empty(result.Errores);
        Assert.NotNull(await db.Runners.AsNoTracking().FirstOrDefaultAsync(r => r.RaceId == race.Id && r.Dorsal == "701"));
        Assert.NotNull(await db.Runners.AsNoTracking().FirstOrDefaultAsync(r => r.RaceId == race.Id && r.Dorsal == "702"));
    }

    // registration-review spec.md "Partial success on mixed valid and invalid rows".
    [Fact]
    public async Task ConfirmBulkAsync_UnaFilaConDorsalVacio_ConfirmaLasDemasYReportaSoloEsaFila()
    {
        using var db = BuildMigratedDbContext();
        var (race, raceCategory, admin) = await SeedRaceAsync(db, "BULK2", capacidad: 10);

        var reg1 = ComprobanteSubidoRegistration(race.Id, raceCategory.Id, "Corredor Uno");
        var reg2 = ComprobanteSubidoRegistration(race.Id, raceCategory.Id, "Corredor Dos");
        db.Registrations.AddRange(reg1, reg2);
        await db.SaveChangesAsync();
        var reg1Id = reg1.Id;
        var reg2Id = reg2.Id;
        db.ChangeTracker.Clear();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inscripciones");
        sheet.Cell(2, 1).Value = reg1Id;
        sheet.Cell(2, 7).Value = "901";
        sheet.Cell(3, 1).Value = reg2Id;
        sheet.Cell(3, 7).Value = string.Empty;

        using var uploadStream = new MemoryStream();
        workbook.SaveAs(uploadStream);
        uploadStream.Position = 0;

        var runnerService = BuildRunnerService(db);
        var registrationService = BuildRegistrationService(db, runnerService);

        var result = await registrationService.ConfirmBulkAsync(race.Id, uploadStream, admin.Id, CancellationToken.None);

        Assert.Equal(2, result.TotalFilas);
        Assert.Equal(1, result.Confirmadas);
        var error = Assert.Single(result.Errores);
        Assert.Equal(3, error.Fila);
        Assert.Contains("Dorsal vacío", error.Motivo);

        Assert.NotNull(await db.Runners.AsNoTracking().FirstOrDefaultAsync(r => r.RaceId == race.Id && r.Dorsal == "901"));
        var reg2Reloaded = await db.Registrations.AsNoTracking().FirstAsync(r => r.Id == reg2Id);
        Assert.Equal(RegistrationStatus.ComprobanteSubido, reg2Reloaded.Estado);
    }

    // registration-review spec.md "Capacity exhausted mid-batch" (design.md D13): la fila
    // que agota el cupo de UNA categoría no bloquea las filas de OTRA categoría en el mismo
    // lote, ni las ya confirmadas antes del punto de agotamiento se revierten.
    [Fact]
    public async Task ConfirmBulkAsync_CupoAgotadoAMitadDelLote_FallaSoloEsaCategoriaYSigueConOtras()
    {
        using var db = BuildMigratedDbContext();
        var (race, catA, admin) = await SeedRaceAsync(db, "BULK3", capacidad: 1);
        var catB = await AddRaceCategoryAsync(db, race, "BULK3B", capacidad: 10);

        var reg1 = ComprobanteSubidoRegistration(race.Id, catA.Id, "Corredor A1"); // catA, gana el único cupo
        var reg2 = ComprobanteSubidoRegistration(race.Id, catA.Id, "Corredor A2"); // catA, cupo ya agotado
        var reg3 = ComprobanteSubidoRegistration(race.Id, catB.Id, "Corredor B1"); // catB, no debe verse afectado
        db.Registrations.AddRange(reg1, reg2, reg3);
        await db.SaveChangesAsync();
        var reg1Id = reg1.Id;
        var reg2Id = reg2.Id;
        var reg3Id = reg3.Id;
        db.ChangeTracker.Clear();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inscripciones");
        sheet.Cell(2, 1).Value = reg1Id;
        sheet.Cell(2, 7).Value = "801";
        sheet.Cell(3, 1).Value = reg2Id;
        sheet.Cell(3, 7).Value = "802";
        sheet.Cell(4, 1).Value = reg3Id;
        sheet.Cell(4, 7).Value = "803";

        using var uploadStream = new MemoryStream();
        workbook.SaveAs(uploadStream);
        uploadStream.Position = 0;

        var runnerService = BuildRunnerService(db);
        var registrationService = BuildRegistrationService(db, runnerService);

        var result = await registrationService.ConfirmBulkAsync(race.Id, uploadStream, admin.Id, CancellationToken.None);

        Assert.Equal(3, result.TotalFilas);
        Assert.Equal(2, result.Confirmadas); // reg1 (catA) + reg3 (catB)
        var error = Assert.Single(result.Errores);
        Assert.Equal(3, error.Fila); // fila de reg2
        Assert.Contains("cupo máximo", error.Motivo);

        Assert.NotNull(await db.Runners.AsNoTracking().FirstOrDefaultAsync(r => r.RaceId == race.Id && r.Dorsal == "801"));
        Assert.NotNull(await db.Runners.AsNoTracking().FirstOrDefaultAsync(r => r.RaceId == race.Id && r.Dorsal == "803"));
        Assert.Null(await db.Runners.AsNoTracking().FirstOrDefaultAsync(r => r.RaceId == race.Id && r.Dorsal == "802"));

        var reg2Reloaded = await db.Registrations.AsNoTracking().FirstAsync(r => r.Id == reg2Id);
        Assert.Equal(RegistrationStatus.ComprobanteSubido, reg2Reloaded.Estado);
    }
}
