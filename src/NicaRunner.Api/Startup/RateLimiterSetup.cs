using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace NicaRunner.Api.Startup;

public static class RateLimiterSetup
{
    // Rate limiting para los endpoints sin autenticación más expuestos a abuso:
    // login/forgot-password (fuerza bruta, agotar cuota de envío de email),
    // resultados públicos (fuerza bruta del token) e inscripción pública
    // (spam de inscripciones). Particionado por IP del cliente, fixed window,
    // sin cola — de nada sirve encolar un login.
    public static void AddNicaRunnerRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
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

            var authSection = configuration.GetSection("RateLimiting:Auth");
            options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                PartitionByIp(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authSection.GetValue("PermitLimit", 8),
                    Window = TimeSpan.FromSeconds(authSection.GetValue("WindowSeconds", 60)),
                    QueueLimit = 0,
                }));

            var publicResultsSection = configuration.GetSection("RateLimiting:PublicResults");
            options.AddPolicy("public-results", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                PartitionByIp(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = publicResultsSection.GetValue("PermitLimit", 30),
                    Window = TimeSpan.FromSeconds(publicResultsSection.GetValue("WindowSeconds", 60)),
                    QueueLimit = 0,
                }));

            // public-runner-registration-manual-payment: la sección "RateLimiting:PublicRegistration"
            // de appsettings*.json y su ajuste fino llegan en Phase 4 (tasks.md 4.2) — la policy se
            // registra ya en Phase 2 (con los mismos defaults conservadores que "public-results")
            // porque [EnableRateLimiting] sobre una policy no registrada lanza en tiempo de
            // request, no de arranque, lo que rompería PublicRegistrationController desde ya.
            var publicRegistrationSection = configuration.GetSection("RateLimiting:PublicRegistration");
            options.AddPolicy("public-registration", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                PartitionByIp(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = publicRegistrationSection.GetValue("PermitLimit", 20),
                    Window = TimeSpan.FromSeconds(publicRegistrationSection.GetValue("WindowSeconds", 60)),
                    QueueLimit = 0,
                }));
        });
    }
}
