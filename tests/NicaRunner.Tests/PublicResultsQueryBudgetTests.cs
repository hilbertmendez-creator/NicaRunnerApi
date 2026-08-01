using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;

namespace NicaRunner.Tests;

// design.md Decisión 3: el detalle público por shareKey reemplaza el materializado
// completo de GetAllByRaceAsync por 3 consultas indexadas y acotadas. Contra Sqlite
// in-memory real (mismo patrón que AliasIntegrationTests) con un interceptor que cuenta
// comandos SQL realmente ejecutados — un mock de los repositorios no puede demostrar
// "una sola consulta" ni "3 consultas en total", solo que el resultado es correcto.
public class PublicResultsQueryBudgetTests
{
    // Cuenta los DbCommand que EF realmente manda a Sqlite — la única forma honesta de
    // probar "esto es UNA consulta" en vez de confiar en la forma del LINQ.
    private sealed class QueryCountingInterceptor : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
            System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<System.Data.Common.DbDataReader> result)
        {
            Count++;
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<System.Data.Common.DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private static (NicaRunnerDbContext Db, QueryCountingInterceptor Interceptor) BuildMigratedDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var interceptor = new QueryCountingInterceptor();
        var options = new DbContextOptionsBuilder<NicaRunnerDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        var db = new NicaRunnerDbContext(options);
        db.Database.Migrate();
        return (db, interceptor);
    }

    private static async Task<(Race Race, Category Cat1, Category Cat2)> SeedRaceAsync(NicaRunnerDbContext db)
    {
        var admin = new User { Email = "admin@x.com", Nombre = "Admin", Role = UserRole.Administrador };
        var capturista = new User { Email = "cap@x.com", Nombre = "Capturista", Role = UserRole.Capturista };
        var cat1 = new Category { Codigo = "5K", NombreCategoria = "5K", Distancia = 5 };
        var cat2 = new Category { Codigo = "10K", NombreCategoria = "10K", Distancia = 10 };
        db.AddRange(admin, capturista, cat1, cat2);
        await db.SaveChangesAsync();

        var race = new Race { Nombre = "Carrera", FechaCarrera = DateTime.UtcNow, JoinCode = "JC", AdminId = admin.Id };
        db.Races.Add(race);
        await db.SaveChangesAsync();

        return (race, cat1, cat2);
    }

    private static Runner NewRunner(int raceId, int categoryId, string dorsal, string? shareKey = null) => new()
    {
        RaceId = raceId, CategoryId = categoryId, Nombre = $"Runner {dorsal}", Dorsal = dorsal, PublicShareKey = shareKey
    };

    private static Result NewResult(int raceId, int runnerId, int categoryId, int capturistaId, DateTime tiempo) => new()
    {
        RaceId = raceId, RunnerId = runnerId, CategoryId = categoryId, CapturistaId = capturistaId,
        Dorsal = "x", TiempoLlegada = tiempo, Posicion = 0
    };

    [Fact]
    public async Task GetPlacingCountsAsync_CalculaLosCuatroAgregadosEnUnaSolaConsulta()
    {
        var (db, interceptor) = BuildMigratedDbContext();
        using var _ = db;
        var (race, cat1, cat2) = await SeedRaceAsync(db);
        var capturista = await db.Users.FirstAsync(u => u.Role == UserRole.Capturista);

        var r1 = NewRunner(race.Id, cat1.Id, "1");
        var r2 = NewRunner(race.Id, cat1.Id, "2");
        var r3 = NewRunner(race.Id, cat2.Id, "3");
        db.Runners.AddRange(r1, r2, r3);
        await db.SaveChangesAsync();

        var t0 = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var res1 = NewResult(race.Id, r1.Id, cat1.Id, capturista.Id, t0);
        var res2 = NewResult(race.Id, r2.Id, cat1.Id, capturista.Id, t0.AddMinutes(5));
        var res3 = NewResult(race.Id, r3.Id, cat2.Id, capturista.Id, t0.AddMinutes(-2));
        db.Results.AddRange(res1, res2, res3);
        await db.SaveChangesAsync();

        var repository = new ResultRepository(db);

        var before = interceptor.Count;
        var counts = await repository.GetPlacingCountsAsync(race.Id, cat1.Id, res2.TiempoLlegada, res2.Id);

        Assert.Equal(1, interceptor.Count - before);
        Assert.Equal(1, counts.CategoryAhead); // res1 llegó antes que res2 en la misma categoría
        Assert.Equal(2, counts.CategoryTotal); // res1 + res2 en cat1
        Assert.Equal(2, counts.RaceAhead); // res1 y res3 llegaron antes que res2 en toda la carrera
        Assert.Equal(3, counts.RaceTotal); // los tres resultados de la carrera
    }

    [Fact]
    public async Task GetPlacingCountsAsync_EnEmpateDeTiempo_DesempataPorIdMenor()
    {
        var (db, _) = BuildMigratedDbContext();
        using var _db = db;
        var (race, cat1, _) = await SeedRaceAsync(db);
        var capturista = await db.Users.FirstAsync(u => u.Role == UserRole.Capturista);

        var r1 = NewRunner(race.Id, cat1.Id, "1");
        var r2 = NewRunner(race.Id, cat1.Id, "2");
        db.Runners.AddRange(r1, r2);
        await db.SaveChangesAsync();

        var tEmpate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var res1 = NewResult(race.Id, r1.Id, cat1.Id, capturista.Id, tEmpate); // Id menor, se captura primero
        db.Results.Add(res1);
        await db.SaveChangesAsync();
        var res2 = NewResult(race.Id, r2.Id, cat1.Id, capturista.Id, tEmpate); // mismo tiempo, Id mayor
        db.Results.Add(res2);
        await db.SaveChangesAsync();

        var repository = new ResultRepository(db);

        var countsRes2 = await repository.GetPlacingCountsAsync(race.Id, cat1.Id, res2.TiempoLlegada, res2.Id);
        var countsRes1 = await repository.GetPlacingCountsAsync(race.Id, cat1.Id, res1.TiempoLlegada, res1.Id);

        Assert.Equal(1, countsRes2.CategoryAhead); // res1 (Id menor) va antes en el empate
        Assert.Equal(0, countsRes1.CategoryAhead); // res1 no tiene a nadie antes
    }

    [Fact]
    public async Task DetalleDeCorredorPorShareKey_ElFlujoCompletoCuestaExactamenteTresConsultas()
    {
        var (db, interceptor) = BuildMigratedDbContext();
        using var _ = db;
        var (race, cat1, _) = await SeedRaceAsync(db);
        var capturista = await db.Users.FirstAsync(u => u.Role == UserRole.Capturista);

        var runner = NewRunner(race.Id, cat1.Id, "1", shareKey: "claveDePruebaXXXXXXXXX");
        db.Runners.Add(runner);
        await db.SaveChangesAsync();

        var result = NewResult(race.Id, runner.Id, cat1.Id, capturista.Id, DateTime.UtcNow);
        db.Results.Add(result);
        await db.SaveChangesAsync();

        var runnerRepository = new RunnerRepository(db);
        var resultRepository = new ResultRepository(db);

        var before = interceptor.Count;

        var foundRunner = await runnerRepository.GetByShareKeyAsync("claveDePruebaXXXXXXXXX");
        Assert.NotNull(foundRunner);

        var foundResult = await resultRepository.GetByRunnerWithCategoryAsync(foundRunner!.RaceId, foundRunner.Id);
        Assert.NotNull(foundResult);

        await resultRepository.GetPlacingCountsAsync(foundRunner.RaceId, foundResult!.CategoryId!.Value, foundResult.TiempoLlegada, foundResult.Id);

        Assert.Equal(3, interceptor.Count - before);
    }
}
