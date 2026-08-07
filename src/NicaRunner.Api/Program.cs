using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using NicaRunner.Api.Dev;
using NicaRunner.Api.Hubs;
using NicaRunner.Api.Middleware;
using NicaRunner.Application.Admin;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Controversies;
using NicaRunner.Application.Dashboard;
using NicaRunner.Application.Notifications;
using NicaRunner.Application.PublicResults;
using NicaRunner.Application.Races;
using NicaRunner.Application.Results;
using NicaRunner.Application.Runners;
using NicaRunner.Application.Users;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Excel;
using NicaRunner.Infrastructure.Notifications;
using NicaRunner.Infrastructure.Repositories;
using NicaRunner.Infrastructure.Security;
using NicaRunner.Infrastructure.Seed;

// Bootstrap logger para capturar errores del arranque MISMO antes de que el
// host esté armado (ej. una falla al leer appsettings). Se reemplaza más abajo
// por el logger definitivo configurado desde builder.Host.UseSerilog.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog reemplaza el logger default de ASP.NET. Config declarativa desde
// appsettings.json (sección "Serilog") para poder ajustar levels sin
// recompilar. Enriquecimiento con MachineName + ThreadId + EnvironmentName +
// contexto de logs. Sink: stdout con CLEF (Compact Log Event Format) que
// Render captura tal cual y cualquier log aggregator (Datadog, Grafana Loki,
// Better Stack) parsea nativamente.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    .WriteTo.Console(new CompactJsonFormatter()));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NicaRunner API",
        Version = "v1",
        Description = "REST API for athletics race timing, results capture, and notifications."
    });

    var apiServerUrl = builder.Configuration["Docs:ApiServerUrl"];
    if (!string.IsNullOrWhiteSpace(apiServerUrl))
    {
        options.AddServer(new OpenApiServer { Url = apiServerUrl.TrimEnd('/') });
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token from POST /api/auth/login"
    });
});
builder.Services.AddSignalR();

// URL versioning con dos rutas por controller: la legacy `/api/{resource}` y la
// nueva `/api/v{version:apiVersion}/{resource}`. Cuando el cliente no especifica
// versión (URL legacy), asumimos 1.0 por default. Cuando aparezca un breaking
// change se creará /api/v2/{resource} y esta configuración crece con
// [ApiVersion("2.0")] adicional. La app Android en Play Store sigue funcionando
// contra /api/{resource} indefinidamente — es la garantía que motivó agregar
// versioning ahora.
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    // "v1" en vez de "v1.0" en la URL — cliente-friendlier.
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

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
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection("Frontend"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRaceRepository, RaceRepository>();
builder.Services.AddScoped<IRaceCategoryRepository, RaceCategoryRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IRunnerRepository, RunnerRepository>();
builder.Services.AddScoped<IResultRepository, ResultRepository>();
builder.Services.AddScoped<IResultAuditRepository, ResultAuditRepository>();
builder.Services.AddScoped<IControversyRepository, ControversyRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IExcelRunnerParser, ExcelRunnerParser>();
builder.Services.AddScoped<IPublicResultTokenRepository, PublicResultTokenRepository>();
builder.Services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddHttpClient<ResendEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<ResendEmailSender>());
builder.Services.AddScoped<INotificationSender, StubWhatsAppSender>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IRefreshTokenCleanupService, RefreshTokenCleanupService>();
builder.Services.AddScoped<IPublicTokenCleanupService, PublicTokenCleanupService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<IRaceCategoryService, RaceCategoryService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IRunnerService, RunnerService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IControversyService, ControversyService>();
builder.Services.AddScoped<IPublicResultService, PublicResultService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRaceDashboardNotifier, RaceDashboardNotifier>();

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

        // El cliente de SignalR (browser) no puede mandar el header Authorization en
        // el handshake de WebSocket, así que manda el JWT como query string
        // "?access_token=...". Solo se acepta ahí, y solo para el path del hub —
        // el resto de la API sigue exigiendo el header Authorization normal.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
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

// En Render (ver render.yaml) esta API corre detrás de su balanceador: la IP
// que ve Kestrel (RemoteIpAddress) es siempre la del proxy de Render, no la
// del cliente real. Sin esto, el particionado por IP del rate limiter de
// abajo agruparía a TODOS los usuarios bajo una sola IP y un solo abusivo
// bloquearía a todos los demás. Render es el único punto de entrada de este
// servicio (no es alcanzable saltándose su borde), así que confiar en
// X-Forwarded-For de cualquier proxy es seguro acá.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Rate limiting para los endpoints sin autenticación más expuestos a abuso:
// login/forgot-password (fuerza bruta, agotar cuota de envío de Resend) y
// resultados públicos (fuerza bruta del token). Particionado por IP del
// cliente, fixed window, sin cola — de nada sirve encolar un login.
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();

        var problem = new
        {
            status = StatusCodes.Status429TooManyRequests,
            title = HttpStatusCode.TooManyRequests.ToString(),
            detail = "Demasiadas solicitudes. Intenta de nuevo en unos momentos.",
        };
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(problem), ct);
    };

    string PartitionByIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    var authSection = builder.Configuration.GetSection("RateLimiting:Auth");
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        PartitionByIp(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authSection.GetValue("PermitLimit", 8),
            Window = TimeSpan.FromSeconds(authSection.GetValue("WindowSeconds", 60)),
            QueueLimit = 0,
        }));

    var publicResultsSection = builder.Configuration.GetSection("RateLimiting:PublicResults");
    options.AddPolicy("public-results", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        PartitionByIp(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = publicResultsSection.GetValue("PermitLimit", 30),
            Window = TimeSpan.FromSeconds(publicResultsSection.GetValue("WindowSeconds", 60)),
            QueueLimit = 0,
        }));
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

// Seed idempotente de administradores de backoffice — corre en ambos entornos:
// en prod para poblar la BD real (una sola vez), en dev para poder probar el
// login localmente. Sin Seed:DefaultAdminPassword configurada, no hace nada.
// Se envuelve en try/catch porque en un checkout de dev fresco (sin migrar
// todavía con `dotnet ef database update`) la tabla Users no existe aún — no
// queremos que eso tumbe el arranque completo del servidor.
using (var seedScope = app.Services.CreateScope())
{
    try
    {
        var seedUserRepository = seedScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var seedPasswordHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var defaultAdminPassword = builder.Configuration["Seed:DefaultAdminPassword"];
        await AdminUserSeeder.SeedAsync(seedUserRepository, seedPasswordHasher, defaultAdminPassword);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "No se pudo ejecutar el seed de administradores (¿faltan migraciones por aplicar?).");
    }
}

// Log estructurado 1-por-request antes que cualquier otro middleware para
// capturar TODOS los requests (incluso los que rechaza HTTPS redirect, CORS,
// auth). Enriquecemos con UserId cuando el usuario está autenticado — es la
// pregunta más común al debuggear "qué hizo este usuario en el evento".
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} en {Elapsed:0.0}ms";
    options.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("Host", http.Request.Host.Value);
        diag.Set("Scheme", http.Request.Scheme);
        // ClaimTypes.NameIdentifier viene poblado desde el JWT (ver
        // JwtTokenGenerator). Cuando el endpoint es público o el token no
        // vino/expiró, User.Identity está desautenticado y el claim es null.
        var userId = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            diag.Set("UserId", userId);
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Debe ir antes que cualquier middleware que dependa de la IP/esquema real
// (rate limiting, HTTPS redirect) para que reflejen al cliente y no al
// balanceador de Render.
app.UseForwardedHeaders();

// Headers de seguridad básicos. Sin CSP a propósito: esta API no sirve HTML
// propio (el frontend es un SPA hospedado aparte); una CSP acá no protege
// nada real y complicaría Swagger UI en dev sin necesidad.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    EmailPreviewEndpoints.Map(app);
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();
app.MapHub<RaceDashboardHub>("/hubs/race-dashboard");

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
