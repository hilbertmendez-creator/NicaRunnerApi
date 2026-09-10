using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Tests;

// backoffice-user-status-toggle PR3 (design.md D1): arranca Program.cs real (pipeline de
// JwtBearerEvents incluido) contra una Sqlite en memoria propia, para probar el código de
// estado HTTP que JwtBearerHandler escribe tras context.Fail() -- un test unitario
// aislado contra AccountStatusJwtEvents no puede confirmarlo.
public class AccountStatusWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public const string TestJwtKey = "account-status-test-signing-key-32-chars-minimum";
    public const string TestJwtIssuer = "NicaRunner.Api.Tests";
    public const string TestJwtAudience = "NicaRunner.Clients.Tests";

    // Parámetro de constructor, no propiedad: un object initializer corre el setter
    // DESPUÉS del constructor -- demasiado tarde para una variable de entorno que
    // Program.cs necesita leer ya puesta (ver nota abajo).
    public AccountStatusWebApplicationFactory(bool enforcePerRequest = true)
    {
        _connection.Open();

        // Program.cs lee Jwt:Key/Issuer/Audience de forma EAGER (antes de builder.Build())
        // -- un override vía ConfigureAppConfiguration llega tarde (confirmado: firma de
        // JWT inválida contra la Key equivocada). Las variables de entorno sí están a
        // tiempo, agregadas de forma síncrona por WebApplication.CreateBuilder(args) --
        // mismo truco que render.yaml en prod. Database:Provider y AccountStatus:CacheSeconds
        // no hace falta pisarlos: Development ya default-ea a Sqlite, y CacheSeconds ya
        // default-ea a 30 en AccountStatusOptions.
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtAudience);
        Environment.SetEnvironmentVariable("AccountStatus__EnforcePerRequest", enforcePerRequest ? "true" : "false");
    }

    // "Development", no Production: Program.cs corre DatabaseMigrator con
    // pg_advisory_lock (Postgres-only) fuera de Development y rompería contra Sqlite.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<NicaRunnerDbContext>>();
            services.AddDbContext<NicaRunnerDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
