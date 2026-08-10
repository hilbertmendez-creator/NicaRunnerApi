using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Admin;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;

namespace NicaRunner.Tests;

// Mismo enfoque que RefreshTokenCleanupServiceTests: el barrido termina en
// EF.ExecuteDeleteAsync (SQL puro), así que un mock del repositorio no probaría
// lo único que importa acá — que el WHERE se lleve también los enlaces
// revocados a mano y no solo los vencidos por fecha.
public class PublicTokenCleanupServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly NicaRunnerDbContext _db;

    public PublicTokenCleanupServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NicaRunnerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new NicaRunnerDbContext(options);
        _db.Database.EnsureCreated();

        // Usuario y carrera base para satisfacer las FK de PublicResultToken.
        _db.Users.Add(new User { Id = 1, Email = "a@b.com", Nombre = "A", Role = UserRole.Administrador });
        _db.Races.Add(new Race { Id = 1, Nombre = "Carrera Test", FechaCarrera = new DateTime(2026, 6, 1), AdminId = 1 });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private PublicTokenCleanupService BuildService() =>
        new(new PublicResultTokenRepository(_db));

    private static PublicResultToken NewToken(string token, DateTime fechaExpiracion, bool revocado = false) => new()
    {
        RaceId = 1,
        Token = token,
        FechaExpiracion = fechaExpiracion,
        IsExpired = revocado,
        CreatedBy = 1,
    };

    [Fact]
    public async Task Cleanup_EnlaceVigente_NoSeBorra()
    {
        _db.PublicResultTokens.Add(NewToken("vigente", DateTime.UtcNow.AddDays(20)));
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, await _db.PublicResultTokens.CountAsync());
    }

    [Fact]
    public async Task Cleanup_EnlaceVencidoPorFecha_SeBorra()
    {
        _db.PublicResultTokens.Add(NewToken("vencido", DateTime.UtcNow.AddMinutes(-1)));
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(1, result.Deleted);
        Assert.Equal(0, await _db.PublicResultTokens.CountAsync());
    }

    // El caso que motivó extender el WHERE: un enlace revocado a mano ya no
    // sirve para nada, así que no tiene por qué esperar sentado hasta su fecha
    // de vencimiento natural para desaparecer de la tabla.
    [Fact]
    public async Task Cleanup_EnlaceRevocadoConFechaFutura_SeBorra()
    {
        _db.PublicResultTokens.Add(NewToken("revocado", DateTime.UtcNow.AddDays(20), revocado: true));
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(1, result.Deleted);
        Assert.Equal(0, await _db.PublicResultTokens.CountAsync());
    }

    [Fact]
    public async Task Cleanup_Mezcla_BorraSoloLosQueYaNoSirven()
    {
        _db.PublicResultTokens.AddRange(
            NewToken("v1", DateTime.UtcNow.AddDays(20)),                       // vigente
            NewToken("v2", DateTime.UtcNow.AddDays(5)),                        // vigente
            NewToken("e1", DateTime.UtcNow.AddMinutes(-10)),                   // vencido
            NewToken("r1", DateTime.UtcNow.AddDays(15), revocado: true),       // revocado
            NewToken("er1", DateTime.UtcNow.AddDays(-2), revocado: true));     // vencido y revocado
        await _db.SaveChangesAsync();

        var result = await BuildService().RunAsync();

        Assert.Equal(3, result.Deleted); // e1 + r1 + er1
        var restantes = await _db.PublicResultTokens.Select(t => t.Token).OrderBy(t => t).ToListAsync();
        Assert.Equal(new[] { "v1", "v2" }, restantes);
    }
}
