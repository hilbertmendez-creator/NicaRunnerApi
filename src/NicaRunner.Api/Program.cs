using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NicaRunner.Api.Middleware;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Dashboard;
using NicaRunner.Application.Notifications;
using NicaRunner.Application.PublicResults;
using NicaRunner.Application.Races;
using NicaRunner.Application.Results;
using NicaRunner.Application.Runners;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Excel;
using NicaRunner.Infrastructure.Notifications;
using NicaRunner.Infrastructure.Repositories;
using NicaRunner.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var useSqlite = builder.Environment.IsDevelopment();

builder.Services.AddDbContext<NicaRunnerDbContext>(options =>
{
    if (useSqlite)
    {
        var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection")
            ?? "Data Source=nicarunner.dev.db";
        options.UseSqlite(sqliteConn);
    }
    else
    {
        var pgConn = builder.Configuration.GetConnectionString("PostgresConnection")
            ?? throw new InvalidOperationException("Falta PostgresConnection en producción");
        options.UseNpgsql(NormalizePostgresConnectionString(pgConn));
    }
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection("Resend"));
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuth"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRaceRepository, RaceRepository>();
builder.Services.AddScoped<IRaceCategoryRepository, RaceCategoryRepository>();
builder.Services.AddScoped<IRunnerRepository, RunnerRepository>();
builder.Services.AddScoped<IResultRepository, ResultRepository>();
builder.Services.AddScoped<IResultAuditRepository, ResultAuditRepository>();
builder.Services.AddScoped<IExcelRunnerParser, ExcelRunnerParser>();
builder.Services.AddScoped<IPublicResultTokenRepository, PublicResultTokenRepository>();
builder.Services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
builder.Services.AddHttpClient<ResendEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<ResendEmailSender>());
builder.Services.AddScoped<INotificationSender, StubWhatsAppSender>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<IRaceCategoryService, RaceCategoryService>();
builder.Services.AddScoped<IRunnerService, RunnerService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IPublicResultService, PublicResultService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });

builder.Services.AddAuthorization();

const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Auto-aplica migraciones pendientes al arrancar en producción (Render no da
// acceso a shell fácil para correr `dotnet ef database update` antes de cada
// deploy). En desarrollo se sigue usando `dotnet ef database update` manual
// contra sqlite. Verificado contra un Postgres real antes de habilitar esto.
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Sin auth, sin tocar la BD: usado por Render (y cualquier monitor externo)
// para el health check del servicio.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

/// <summary>
/// Render expone la connection string de su Postgres administrado en formato
/// URI (postgres://usuario:password@host:puerto/db), pero Npgsql solo
/// entiende el formato keyword=value (Host=...;Username=...;...). Sin esto,
/// NpgsqlConnectionStringBuilder lanza ArgumentException apenas arranca el
/// contenedor ("Format of the initialization string does not conform to
/// specification starting at index 0") — verificado en el primer deploy real
/// a Render. Si la cadena ya viene en formato keyword=value (como en dev
/// contra un Postgres local), se devuelve sin tocar.
/// </summary>
static string NormalizePostgresConnectionString(string connectionString)
{
    if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.TrimStart('/');
    var port = uri.Port == -1 ? 5432 : uri.Port;

    // Prefer (no Require): Render expone Postgres con SSL, pero exigirlo
    // rompería contra un Postgres local sin SSL (ej. Docker en dev/test).
    return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
}
