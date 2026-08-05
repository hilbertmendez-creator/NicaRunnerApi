using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Registrations;
using NicaRunner.Application.Registrations.Dtos;
using NicaRunner.Application.ReservedDorsals;
using NicaRunner.Application.ReservedDorsals.Dtos;
using NicaRunner.Application.Runners;
using NicaRunner.Application.Runners.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;

namespace NicaRunner.Tests;

// tasks.md 2.10: integración contra Sqlite real (mismo harness que
// DorsalNormalizationIntegrationTests/AliasIntegrationTests) para el alcance que un mock
// no cubre: los conditional UPDATE atómicos (TryReserveSlotAsync/TryClaimAsync) y el
// índice único aditivo realmente aplicando sobre datos persistidos.
//
// "Concurrent confirmations at the capacity boundary" se prueba con llamadas
// SECUENCIALES, no Task.WhenAll: DbContext no es thread-safe para operaciones paralelas
// sobre la misma instancia, y el codebase ya resuelve "condición de carrera" simulando el
// resultado ganador/perdedor en vez de concurrencia real de hilos (ver
// ResultServiceIdempotencyTests.Create_KeyConcurrentRace_RelesYDevuelveGanador). El
// contrato observable que importa —el WHERE de la conditional UPDATE nunca deja pasar más
// de Capacidad filas— es exactamente lo que estas llamadas secuenciales verifican.
public class RegistrationServiceIntegrationTests
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

    private static async Task<(Race Race, Category Category, RaceCategory RaceCategory, User Admin)> SeedRaceAsync(
        NicaRunnerDbContext db, string suffix, int? capacidad = null)
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

        return (race, category, raceCategory, admin);
    }

    private static RunnerService BuildRunnerService(NicaRunnerDbContext db) => new(
        new RunnerRepository(db),
        new RaceRepository(db),
        new RaceCategoryRepository(db),
        new Mock<IExcelRunnerParser>().Object,
        new ReservedDorsalRepository(db));

    private static RegistrationService BuildRegistrationService(
        NicaRunnerDbContext db, RunnerService runnerService, IExcelRegistrationParser? excelParser = null) => new(
        new RegistrationRepository(db),
        new RegistrationLinkRepository(db),
        new RaceRepository(db),
        new RaceCategoryRepository(db),
        runnerService,
        new AuditService(new AuditLogRepository(db)),
        Options.Create(new RegistrationOptions()),
        excelParser ?? new Mock<IExcelRegistrationParser>().Object);

    private static Registration ComprobanteSubidoRegistration(int raceId, int raceCategoryId, string nombre) => new()
    {
        RaceId = raceId,
        RaceCategoryId = raceCategoryId,
        Nombre = nombre,
        FechaNacimiento = new DateTime(1990, 1, 1),
        Estado = RegistrationStatus.ComprobanteSubido,
        ReferenciaTransferencia = "REF-1"
    };

    // ---------- Capacity boundary (D2/D13) ----------

    [Fact]
    public async Task TryReserveSlotAsync_ArribaDeCapacidad_NuncaExcedeElLimite()
    {
        using var db = BuildMigratedDbContext();
        var (_, _, raceCategory, _) = await SeedRaceAsync(db, "CAP", capacidad: 1);

        var repository = new RegistrationRepository(db);

        var primero = await repository.TryReserveSlotAsync(raceCategory.Id);
        var segundo = await repository.TryReserveSlotAsync(raceCategory.Id);

        Assert.Equal(1, primero);
        Assert.Equal(0, segundo);

        var reloaded = await db.RaceCategories.AsNoTracking().FirstAsync(rc => rc.Id == raceCategory.Id);
        Assert.Equal(1, reloaded.ConfirmedCount);
    }

    [Fact]
    public async Task ConfirmAsync_DosInscripcionesConUnSoloCupo_SoloUnaSeConfirma()
    {
        using var db = BuildMigratedDbContext();
        var (race, _, raceCategory, admin) = await SeedRaceAsync(db, "CONF", capacidad: 1);

        var reg1 = ComprobanteSubidoRegistration(race.Id, raceCategory.Id, "Corredor Uno");
        var reg2 = ComprobanteSubidoRegistration(race.Id, raceCategory.Id, "Corredor Dos");
        db.Registrations.AddRange(reg1, reg2);
        await db.SaveChangesAsync();
        var reg1Id = reg1.Id;
        var reg2Id = reg2.Id;

        // TryClaimAsync/TryReserveSlotAsync escriben con ExecuteUpdateAsync (SQL crudo)
        // que NO refresca instancias ya trackeadas — sin este Clear(), el reg1/reg2
        // recién sembrado seguiría viéndose "ComprobanteSubido" en memoria después de
        // confirmarlo, aunque la fila real en la base ya diga "Confirmada". Simula el
        // DbContext-por-request fresco que existe en producción.
        db.ChangeTracker.Clear();

        var runnerService = BuildRunnerService(db);
        var registrationService = BuildRegistrationService(db, runnerService);

        var confirmado1 = await registrationService.ConfirmAsync(race.Id, reg1Id, new ConfirmRegistrationRequest("101"), admin.Id, CancellationToken.None);
        Assert.Equal(RegistrationStatus.Confirmada, confirmado1.Estado);
        Assert.NotNull(confirmado1.RunnerId);

        db.ChangeTracker.Clear();

        // design.md D13: CapacityExhaustedException (ConflictException subtype).
        await Assert.ThrowsAsync<CapacityExhaustedException>(() =>
            registrationService.ConfirmAsync(race.Id, reg2Id, new ConfirmRegistrationRequest("102"), admin.Id, CancellationToken.None));

        // reg2 nunca quedó "atascada" en Confirmada sin Runner — el compensating release
        // la devolvió a ComprobanteSubido (design.md D3: sesga a undersell, nunca oversell).
        var reg2Reloaded = await db.Registrations.AsNoTracking().FirstAsync(r => r.Id == reg2.Id);
        Assert.Equal(RegistrationStatus.ComprobanteSubido, reg2Reloaded.Estado);
        Assert.Null(reg2Reloaded.RunnerId);

        Assert.Equal(1, await db.Runners.CountAsync(r => r.RaceId == race.Id));
    }

    [Fact]
    public async Task ConfirmAsync_DobleConfirmacionDeLaMismaInscripcion_LaSegundaEsRechazada()
    {
        using var db = BuildMigratedDbContext();
        var (race, _, raceCategory, admin) = await SeedRaceAsync(db, "IDEM", capacidad: 5);

        var reg = ComprobanteSubidoRegistration(race.Id, raceCategory.Id, "Corredor Idem");
        db.Registrations.Add(reg);
        await db.SaveChangesAsync();
        var regId = reg.Id;
        db.ChangeTracker.Clear(); // ver comentario equivalente arriba

        var runnerService = BuildRunnerService(db);
        var registrationService = BuildRegistrationService(db, runnerService);

        await registrationService.ConfirmAsync(race.Id, regId, new ConfirmRegistrationRequest("201"), admin.Id, CancellationToken.None);
        db.ChangeTracker.Clear();

        // Idempotency latch (design.md Data Flow, "Step 1 is the idempotency latch"):
        // el segundo confirm sobre la MISMA fila ya Confirmada pierde el rows-affected
        // de TryClaimAsync y nunca llega a crear un segundo Runner.
        await Assert.ThrowsAsync<ConflictException>(() =>
            registrationService.ConfirmAsync(race.Id, regId, new ConfirmRegistrationRequest("999"), admin.Id, CancellationToken.None));

        Assert.Equal(1, await db.Runners.CountAsync(r => r.RaceId == race.Id));
        Assert.Equal(1, (await db.RaceCategories.AsNoTracking().FirstAsync(rc => rc.Id == raceCategory.Id)).ConfirmedCount);
    }

    // ---------- Full anonymous submit -> confirm flow ----------

    [Fact]
    public async Task FlujoCompleto_SumisionAnonimaHastaConfirmacion_CreaRunnerConDorsalYAuditLog()
    {
        using var db = BuildMigratedDbContext();
        var (race, category, raceCategory, admin) = await SeedRaceAsync(db, "E2E", capacidad: 10);

        var link = new RegistrationLink
        {
            RaceId = race.Id,
            Token = "token-e2e",
            FechaExpiracion = DateTime.UtcNow.AddDays(30),
            CreatedBy = admin.Id
        };
        db.RegistrationLinks.Add(link);
        await db.SaveChangesAsync();
        var raceId = race.Id;
        var adminId = admin.Id;
        var raceCategoryId = raceCategory.Id;
        var categoryId = category.Id;

        // Cada paso simula un request HTTP separado (Submit -> UploadReceipt -> Confirm),
        // cada uno con su propio DbContext en producción — Clear() entre pasos evita que
        // una entidad trackeada de un paso anterior enmascare lo que el siguiente paso
        // realmente persistió (mismo motivo que en los tests de arriba).
        db.ChangeTracker.Clear();

        var runnerService = BuildRunnerService(db);
        var registrationService = BuildRegistrationService(db, runnerService);

        var linkInfo = await registrationService.GetLinkInfoAsync("token-e2e", CancellationToken.None);
        Assert.Equal(raceId, linkInfo.RaceId);
        var opcion = Assert.Single(linkInfo.Categorias);
        Assert.Equal(raceCategoryId, opcion.RaceCategoryId);

        var submitRequest = new SubmitRegistrationRequest(
            "Ana", "Pérez", "555-1234", "ana@x.com", Sexo.F, "Club X",
            new DateTime(1995, 3, 10), raceCategoryId, null, null);
        var submitted = await registrationService.SubmitAsync("token-e2e", submitRequest, CancellationToken.None);
        Assert.Equal(RegistrationStatus.Pendiente, submitted.Estado);
        var submittedId = submitted.Id;

        db.ChangeTracker.Clear();

        var withReceipt = await registrationService.UploadReceiptAsync(
            "token-e2e", submittedId, new UploadReceiptRequest("REF-E2E-1"), CancellationToken.None);
        Assert.Equal(RegistrationStatus.ComprobanteSubido, withReceipt.Estado);

        db.ChangeTracker.Clear();

        var confirmed = await registrationService.ConfirmAsync(
            raceId, submittedId, new ConfirmRegistrationRequest("501"), adminId, CancellationToken.None);

        Assert.Equal(RegistrationStatus.Confirmada, confirmed.Estado);
        Assert.NotNull(confirmed.RunnerId);

        var runner = await db.Runners.AsNoTracking().FirstAsync(r => r.Id == confirmed.RunnerId);
        Assert.Equal("501", runner.Dorsal);
        Assert.Equal("Ana", runner.Nombre);
        Assert.Equal(categoryId, runner.CategoryId);

        var auditEntries = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == AuditEntityTypes.Registration && a.EntityId == submittedId)
            .ToListAsync();
        Assert.Contains(auditEntries, a => a.Campo == "Estado" && a.ValorNuevo == nameof(RegistrationStatus.Confirmada));
    }

    // ---------- Reserved dorsal: create/update guard + D10 exemption + release ----------

    [Fact]
    public async Task RunnerServiceCreateAsync_DorsalReservado_LanzaConflict()
    {
        using var db = BuildMigratedDbContext();
        var (race, category, _, admin) = await SeedRaceAsync(db, "RSV1");

        var reservedDorsalService = new ReservedDorsalService(new ReservedDorsalRepository(db), new RaceRepository(db));
        await reservedDorsalService.CreateAsync(race.Id, new CreateReservedDorsalRequest("300", "VIP"), admin.Id, CancellationToken.None);

        var runnerService = BuildRunnerService(db);
        var request = new CreateRunnerRequest("Beto", null, "300", null, null, null, null, new DateTime(1990, 1, 1), null, category.Id);

        await Assert.ThrowsAsync<ConflictException>(() => runnerService.CreateAsync(race.Id, request, CancellationToken.None));
    }

    [Fact]
    public async Task RunnerServiceCreateAsync_DorsalNoReservado_SigueFuncionando()
    {
        using var db = BuildMigratedDbContext();
        var (race, category, _, admin) = await SeedRaceAsync(db, "RSV2");

        await new ReservedDorsalService(new ReservedDorsalRepository(db), new RaceRepository(db))
            .CreateAsync(race.Id, new CreateReservedDorsalRequest("300", null), admin.Id, CancellationToken.None);

        var runnerService = BuildRunnerService(db);
        var request = new CreateRunnerRequest("Beto", null, "301", null, null, null, null, new DateTime(1990, 1, 1), null, category.Id);

        var dto = await runnerService.CreateAsync(race.Id, request, CancellationToken.None);
        Assert.Equal("301", dto.Dorsal);
    }

    [Fact]
    public async Task RunnerServiceUpdateAsync_CambiaAUnDorsalReservado_LanzaConflict()
    {
        using var db = BuildMigratedDbContext();
        var (race, category, _, admin) = await SeedRaceAsync(db, "RSV3");
        var runnerService = BuildRunnerService(db);

        var created = await runnerService.CreateAsync(race.Id,
            new CreateRunnerRequest("Carlos", null, "400", null, null, null, null, new DateTime(1990, 1, 1), null, category.Id),
            CancellationToken.None);

        await new ReservedDorsalService(new ReservedDorsalRepository(db), new RaceRepository(db))
            .CreateAsync(race.Id, new CreateReservedDorsalRequest("401", null), admin.Id, CancellationToken.None);

        var updateRequest = new UpdateRunnerRequest("Carlos", null, "401", null, null, null, null, new DateTime(1990, 1, 1), null, category.Id);

        await Assert.ThrowsAsync<ConflictException>(() => runnerService.UpdateAsync(race.Id, created.Id, updateRequest, CancellationToken.None));
    }

    // D10: editar un campo no relacionado en un corredor que YA tenía un dorsal que
    // DESPUÉS se reservó no debe fallar — mismo grano que excludeRunnerId.
    [Fact]
    public async Task RunnerServiceUpdateAsync_DorsalPropioSeReservaDespues_EdicionNoRelacionadaNoFalla()
    {
        using var db = BuildMigratedDbContext();
        var (race, category, _, admin) = await SeedRaceAsync(db, "RSV4");
        var runnerService = BuildRunnerService(db);

        var created = await runnerService.CreateAsync(race.Id,
            new CreateRunnerRequest("Diana", null, "500", null, null, null, null, new DateTime(1990, 1, 1), null, category.Id),
            CancellationToken.None);

        // El propio dorsal del corredor se reserva DESPUÉS de haberlo asignado — el
        // corredor ya lo tenía legítimamente.
        await new ReservedDorsalService(new ReservedDorsalRepository(db), new RaceRepository(db))
            .CreateAsync(race.Id, new CreateReservedDorsalRequest("500", null), admin.Id, CancellationToken.None);

        var updateRequest = new UpdateRunnerRequest("Diana Actualizada", null, "500", null, null, null, null, new DateTime(1990, 1, 1), null, category.Id);

        var updated = await runnerService.UpdateAsync(race.Id, created.Id, updateRequest, CancellationToken.None);
        Assert.Equal("Diana Actualizada", updated.Nombre);
        Assert.Equal("500", updated.Dorsal);
    }

    [Fact]
    public async Task ReservedDorsalService_DeleteLuegoReutilizar_Funciona()
    {
        using var db = BuildMigratedDbContext();
        var (race, category, _, admin) = await SeedRaceAsync(db, "RSV5");
        var reservedDorsalService = new ReservedDorsalService(new ReservedDorsalRepository(db), new RaceRepository(db));

        var reserved = await reservedDorsalService.CreateAsync(race.Id, new CreateReservedDorsalRequest("600", null), admin.Id, CancellationToken.None);

        var runnerService = BuildRunnerService(db);
        var request = new CreateRunnerRequest("Erick", null, "600", null, null, null, null, new DateTime(1990, 1, 1), null, category.Id);
        await Assert.ThrowsAsync<ConflictException>(() => runnerService.CreateAsync(race.Id, request, CancellationToken.None));

        await reservedDorsalService.DeleteAsync(race.Id, reserved.Id, CancellationToken.None);

        var dto = await runnerService.CreateAsync(race.Id, request, CancellationToken.None);
        Assert.Equal("600", dto.Dorsal);
    }
}
