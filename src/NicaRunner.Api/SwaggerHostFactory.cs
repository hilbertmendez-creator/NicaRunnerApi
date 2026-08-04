using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using NicaRunner.Api.Startup;

namespace NicaRunner.Api;

/// <summary>
/// Host factory for Swashbuckle CLI (<c>dotnet swagger tofile</c>).
/// Minimal hosting no longer exposes a classic <c>Startup</c> class; the CLI
/// looks for this factory before falling back to WebHost+Startup.
/// Registers controllers + SwaggerGen only — enough for OpenAPI document generation.
/// </summary>
public static class SwaggerHostFactory
{
    public static IHost CreateHost()
    {
        var apiAssembly = typeof(SwaggerHostFactory).Assembly;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
            ApplicationName = apiAssembly.GetName().Name,
        });

        builder.Services
            .AddControllers()
            .AddApplicationPart(apiAssembly)
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

            // Asp.Versioning groups are "v1"; include all descriptors in the single doc.
            options.DocInclusionPredicate((_, _) => true);

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
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        var app = builder.Build();
        app.MapControllers();
        return app;
    }
}
