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
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<RaceJudge> RaceJudges => Set<RaceJudge>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Dorsal único por carrera (no global)
        modelBuilder.Entity<Runner>()
            .HasIndex(r => new { r.RaceId, r.Dorsal })
            .IsUnique();

        // JoinCode único para que /api/races/join pueda resolver una sola carrera
        modelBuilder.Entity<Race>()
            .HasIndex(r => r.JoinCode)
            .IsUnique();

        // Un juez no puede unirse dos veces a la misma carrera
        modelBuilder.Entity<RaceJudge>()
            .HasIndex(j => new { j.RaceId, j.UserId })
            .IsUnique();
        modelBuilder.Entity<RaceJudge>()
            .HasOne(j => j.Race)
            .WithMany(r => r.Judges)
            .HasForeignKey(j => j.RaceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RaceJudge>()
            .HasOne(j => j.User)
            .WithMany()
            .HasForeignKey(j => j.UserId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // RefreshToken: lookup primario por hash (siempre llega el token plano
        // del cliente, lo hasheamos y buscamos). FamilyId indexado para revocar
        // toda la familia de una en el caso replay-detected sin scan completo.
        // Cascade desde User para que dar de baja una cuenta limpie sus tokens.
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.FamilyId);
        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        // Longitudes explícitas: el hash es SHA-256 hex (64 chars), el
        // ReplacedByTokenHash apunta a otro hash. Acotar para evitar columnas
        // "text" gigantes y dejar el contrato claro.
        modelBuilder.Entity<RefreshToken>()
            .Property(t => t.TokenHash)
            .HasMaxLength(64)
            .IsRequired();
        modelBuilder.Entity<RefreshToken>()
            .Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(64);

        // GoogleId único, pero solo aplica a usuarios con cuenta de Google vinculada.
        // Sin corchetes T-SQL: este modelo corre sobre Sqlite (dev) y Postgres (prod).
        modelBuilder.Entity<User>()
            .HasIndex(u => u.GoogleId)
            .IsUnique()
            .HasFilter("\"GoogleId\" IS NOT NULL");

        // Evitar cascade delete accidental en relaciones sensibles (auditoría, resultados)
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Runner)
            .WithMany(ru => ru.Results)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResultAudit>()
            .HasOne(a => a.Result)
            .WithMany(r => r.AuditEntries)
            .OnDelete(DeleteBehavior.Cascade);

        // El historial de notificaciones no debe perderse por cascada si se borra
        // la carrera/corredor/resultado relacionado (no hay endpoints de borrado hoy,
        // pero se deja explícito para no depender del comportamiento por defecto de EF).
        modelBuilder.Entity<NotificationLog>()
            .HasOne(n => n.Race)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NotificationLog>()
            .HasOne(n => n.Runner)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NotificationLog>()
            .HasOne(n => n.Result)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
