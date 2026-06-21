using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NicaRunner.Api.Middleware;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races;
using NicaRunner.Application.Results;
using NicaRunner.Application.Runners;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Excel;
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
        options.UseNpgsql(pgConn);
    }
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRaceRepository, RaceRepository>();
builder.Services.AddScoped<IRaceCategoryRepository, RaceCategoryRepository>();
builder.Services.AddScoped<IRunnerRepository, RunnerRepository>();
builder.Services.AddScoped<IResultRepository, ResultRepository>();
builder.Services.AddScoped<IResultAuditRepository, ResultAuditRepository>();
builder.Services.AddScoped<IExcelRunnerParser, ExcelRunnerParser>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<IRaceCategoryService, RaceCategoryService>();
builder.Services.AddScoped<IRunnerService, RunnerService>();
builder.Services.AddScoped<IResultService, ResultService>();

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

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
