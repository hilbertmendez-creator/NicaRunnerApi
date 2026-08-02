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
using NicaRunner.Api.Auth;
using NicaRunner.Api.Dev;
using NicaRunner.Api.Hubs;
using NicaRunner.Api.Middleware;
using NicaRunner.Api.Startup;
using NicaRunner.Application.Admin;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Controversias;
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
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    });
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
var signalRBuilder = builder.Services.AddSignalR();
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConn))
{
    // Backplane necesario para que los mensajes de SignalR lleguen a clientes
    // conectados a otra instancia cuando la API escala horizontalmente (el
    // plan free de Render corre una sola instancia, así que hoy esto queda
    // inactivo — se activa solo seteando ConnectionStrings__Redis). Ver
    // docs/render-setup.md.
    signalRBuilder.AddStackExchangeRedis(redisConn);
}

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
        options.UseNpgsql(PostgresConnectionStringNormalizer.Normalize(pgConn));
    }
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection("Resend"));
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection("Frontend"));
builder.Services.Configure<LockoutOptions>(builder.Configuration.GetSection("Lockout"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AliasAssigner>();
builder.Services.AddScoped<IRaceRepository, RaceRepository>();
builder.Services.AddScoped<IRaceCategoryRepository, RaceCategoryRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IRunnerRepository, RunnerRepository>();
builder.Services.AddScoped<IResultRepository, ResultRepository>();
builder.Services.AddScoped<IResultAuditRepository, ResultAuditRepository>();
builder.Services.AddScoped<ITimingDisputeRepository, TimingDisputeRepository>();
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
builder.Services.AddScoped<IPublicResultService, PublicResultService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRaceDashboardNotifier, RaceDashboardNotifier>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSigningKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Falta Jwt:Key en la configuración");
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
        };

        // El cliente web (browser) no manda el header Authorization: el JWT
        // viaja en la cookie httpOnly nr_at (ver AuthController). Si no vino
        // el header, se cae a la cookie — cubre tanto la API normal como el
        // handshake de WebSocket del hub de SignalR (el browser adjunta
        // cookies automáticamente ahí también). La app Android y cualquier
        // cliente que sí mande Authorization siguen priorizando el header, sin
        // cambios — este fallback nunca pisa un header ya presente.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization"))
                {
                    var cookieToken = context.Request.Cookies[AuthCookieNames.AccessToken];
                    if (!string.IsNullOrEmpty(cookieToken))
                    {
                        context.Token = cookieToken;
                    }
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
            .AllowCredentials()
            // El frontend (Vercel) y esta API (Render) viven en dominios distintos:
            // JS del frontend no puede leer la cookie nr_csrf vía document.cookie
            // entre dominios, así que se la exponemos en este header para que el
            // cliente la guarde en memoria (ver AuthController.SetAuthCookies y el
            // middleware de eco más abajo).
            .WithExposedHeaders(AuthCookieNames.CsrfHeader);
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
// login/forgot-password (fuerza bruta, agotar cuota de envío de email) y
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
//
// Un rolling deploy puede tener brevemente dos instancias arrancando a la vez
// (la vieja terminando de apagarse, la nueva healthcheck-eando), y ambas
// correrían Migrate() en paralelo contra la misma BD. Se serializa con un
// advisory lock de Postgres: la segunda instancia espera a que la primera
// termine y libere el lock; su propio Migrate() posterior es un no-op seguro
// porque EF Core ya ve las migraciones aplicadas.
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
    await DatabaseMigrator.MigrateWithAdvisoryLockAsync(db);
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
        var seedAliasAssigner = seedScope.ServiceProvider.GetRequiredService<AliasAssigner>();
        var defaultAdminPassword = builder.Configuration["Seed:DefaultAdminPassword"];
        await AdminUserSeeder.SeedAsync(seedUserRepository, seedPasswordHasher, seedAliasAssigner, defaultAdminPassword);

        // M2 (design.md §3.2): backfill de alias para filas creadas antes de esta PR.
        // Mismo scope/repositorio que el seed de arriba — idempotente, seguro en cada deploy.
        await UsernameBackfillService.BackfillAsync(seedUserRepository, seedAliasAssigner);

        // enlaces-publicos-resultados design.md Decisión 2: backfill de PublicShareKey
        // para corredores creados antes de esta migración (o insertados por una
        // instancia vieja durante la ventana de deploy). Mismo scope/patrón que el
        // backfill de arriba — idempotente, seguro en cada boot.
        var seedRunnerRepository = seedScope.ServiceProvider.GetRequiredService<IRunnerRepository>();
        await RunnerShareKeyBackfillService.BackfillAsync(seedRunnerRepository);

        // M3 (design.md §3.2): audita colisiones de email que solo difieren en
        // mayúsculas/minúsculas antes de habilitar cualquier normalización futura a
        // minúsculas. Nunca fusiona nada — solo advierte "en voz alta" para
        // resolución manual (user-auth: "Email Address Normalization").
        var emailCaseCollisions = await EmailCaseDuplicateAuditService.FindCollisionsAsync(seedUserRepository);
        foreach (var collision in emailCaseCollisions)
        {
            app.Logger.LogWarning(
                "Colisión de email por mayúsculas/minúsculas: {NormalizedEmail} en usuarios {UserIds} — " +
                "requiere resolución manual, NO se fusiona automáticamente.",
                collision.NormalizedEmail, string.Join(", ", collision.Users.Select(u => u.UserId)));
        }
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

// CSRF (double-submit cookie) para tráfico autenticado por cookie: si el
// browser manda la cookie nr_at o nr_rt en un request mutante SIN header
// Authorization, exige que el header X-CSRF-Token coincida con la cookie
// nr_csrf (legible por JS a propósito). Clientes que sí mandan Authorization
// (Android, scripts, admin) no dependen de cookies ambient y quedan afuera de
// este chequeo — CSRF ataca cookies que el browser adjunta solo.
var mutatingMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

// Endpoints anónimos que se autentican con credenciales explícitas del body
// (password, google id token, link de reseteo) y no con la cookie ambient:
// no dependen de nr_at/nr_rt para autorizar nada, así que no deben exigir
// CSRF. Sin esto, un usuario con cookies viejas/expiradas de una sesión
// anterior (y el csrfToken en memoria del cliente perdido por haber recargado
// la página) queda bloqueado con 403 al intentar loguearse o pedir un reset
// de contraseña — el propio login/forgot-password es lo que debería dejarlo
// arrancar de cero.
var csrfExemptPaths = new[] { "/auth/login", "/auth/google-login", "/auth/forgot-password", "/auth/reset-password" };
app.Use(async (context, next) =>
{
    var request = context.Request;

    // Eco de la cookie CSRF como header en toda respuesta donde viaje: el
    // frontend (dominio distinto al de esta API) no puede leerla vía
    // document.cookie, así que se la devolvemos acá para que la guarde en
    // memoria y la reenvíe como X-CSRF-Token en el próximo request mutante.
    if (request.Cookies.TryGetValue(AuthCookieNames.Csrf, out var csrfCookieValue) && !string.IsNullOrEmpty(csrfCookieValue))
        context.Response.Headers[AuthCookieNames.CsrfHeader] = csrfCookieValue;

    var usaCookieAuth = !request.Headers.ContainsKey("Authorization") &&
        (request.Cookies.ContainsKey(AuthCookieNames.AccessToken) || request.Cookies.ContainsKey(AuthCookieNames.RefreshToken));

    var isCsrfExempt = csrfExemptPaths.Any(path =>
        request.Path.Value?.EndsWith(path, StringComparison.OrdinalIgnoreCase) == true);

    if (mutatingMethods.Contains(request.Method) && usaCookieAuth && !isCsrfExempt)
    {
        var csrfCookie = request.Cookies[AuthCookieNames.Csrf];
        var csrfHeader = request.Headers[AuthCookieNames.CsrfHeader].ToString();
        if (string.IsNullOrEmpty(csrfCookie) || !string.Equals(csrfCookie, csrfHeader, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                """{"status":403,"title":"Forbidden","detail":"CSRF token invalido o ausente."}""");
            return;
        }
    }

    await next();
});

app.UseRateLimiter();

app.MapControllers();
app.MapHub<RaceDashboardHub>("/hubs/race-dashboard");

// Sin auth, sin tocar la BD: usado por Render (y cualquier monitor externo)
// para el health check del servicio.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
