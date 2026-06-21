using Microsoft.EntityFrameworkCore;
using NicaRunner.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
