using Microsoft.EntityFrameworkCore;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Data;

public class NicaRunnerDbContext : DbContext
{
    public NicaRunnerDbContext(DbContextOptions<NicaRunnerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<RaceCategory> RaceCategories => Set<RaceCategory>();
    public DbSet<Runner> Runners => Set<Runner>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<ResultAudit> ResultAudits => Set<ResultAudit>();
    public DbSet<PublicResultToken> PublicResultTokens => Set<PublicResultToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Dorsal único por carrera (no global)
        modelBuilder.Entity<Runner>()
            .HasIndex(r => new { r.RaceId, r.Dorsal })
            .IsUnique();

        // Token público único
        modelBuilder.Entity<PublicResultToken>()
            .HasIndex(t => t.Token)
            .IsUnique();

        // FK explícito: la convención de EF no detecta CreatedBy como la clave foránea
        // de la navegación Creator (no sigue el patrón "<Navegacion>Id"), y sin esto
        // EF crea una columna sombra "CreatorId" adicional que nunca se rellena.
        modelBuilder.Entity<PublicResultToken>()
            .HasOne(t => t.Creator)
            .WithMany()
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Email único por usuario
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Evitar cascade delete accidental en relaciones sensibles (auditoría, resultados)
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Runner)
            .WithMany(ru => ru.Results)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResultAudit>()
            .HasOne(a => a.Result)
            .WithMany(r => r.AuditEntries)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }
}
