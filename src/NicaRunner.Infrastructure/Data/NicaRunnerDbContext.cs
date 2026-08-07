using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Data;

public class NicaRunnerDbContext : DbContext
{
    public NicaRunnerDbContext(DbContextOptions<NicaRunnerDbContext> options)
        : base(options)
    {
    }

    // Npgsql exige Kind=Unspecified para escribir en timestamp without time zone; nuestro
    // código siempre pasa DateTime.UtcNow (Kind=Utc), que sin esto Npgsql rechaza con
    // NotSupportedException. SpecifyKind no mueve el reloj, solo saca la etiqueta — el
    // valor que se guarda es el mismo instante UTC. Al leer, se vuelve a etiquetar como
    // Utc: coherente con UtcDateTimeConverter (capa JSON), que asume exactamente esto.
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeValueConverter = new(
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public DbSet<User> Users => Set<User>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RaceCategory> RaceCategories => Set<RaceCategory>();
    public DbSet<Runner> Runners => Set<Runner>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<ResultAudit> ResultAudits => Set<ResultAudit>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PublicResultToken> PublicResultTokens => Set<PublicResultToken>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<RaceJudge> RaceJudges => Set<RaceJudge>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TimingDispute> TimingDisputes => Set<TimingDispute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Dorsal único por carrera (no global)
        modelBuilder.Entity<Runner>()
            .HasIndex(r => new { r.RaceId, r.Dorsal })
            .IsUnique();

        // Código de categoría único en el catálogo global
        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Codigo)
            .IsUnique();

        // Una categoría solo puede seleccionarse una vez por carrera
        modelBuilder.Entity<RaceCategory>()
            .HasIndex(rc => new { rc.RaceId, rc.CategoryId })
            .IsUnique();
        modelBuilder.Entity<RaceCategory>()
            .HasOne(rc => rc.Race)
            .WithMany(r => r.Categories)
            .HasForeignKey(rc => rc.RaceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RaceCategory>()
            .HasOne(rc => rc.Category)
            .WithMany(c => c.RaceCategories)
            .HasForeignKey(rc => rc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Los jueces que dieron salida y cierre a la categoría. Restrict: borrar un
        // usuario no puede llevarse por delante la trazabilidad de quién dio el
        // disparo — esa atribución es parte del resultado oficial.
        modelBuilder.Entity<RaceCategory>()
            .HasOne(rc => rc.StartedBy)
            .WithMany()
            .HasForeignKey(rc => rc.StartedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RaceCategory>()
            .HasOne(rc => rc.ClosedBy)
            .WithMany()
            .HasForeignKey(rc => rc.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Estado como string en la BD: una columna que dice "EnCurso" se puede leer en
        // un psql a las 2am durante una carrera; una que dice "1", no.
        modelBuilder.Entity<RaceCategory>()
            .Property(rc => rc.Estado)
            .HasConversion<string>()
            .HasMaxLength(16);

        // No se puede borrar una categoría del catálogo mientras tenga corredores inscritos
        modelBuilder.Entity<Runner>()
            .HasOne(r => r.Category)
            .WithMany(c => c.Runners)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Alias (Username): namespace disjoint del email (nunca contiene '@'), índice
        // único filtrado mientras la columna es nullable — mismo patrón que GoogleId.
        // Se reemplaza por un índice único plano en M4 (PR2) cuando pase a NOT NULL.
        modelBuilder.Entity<User>()
            .Property(u => u.Username)
            .HasMaxLength(30);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique()
            .HasFilter("\"Username\" IS NOT NULL");

        // Evitar cascade delete accidental en relaciones sensibles (auditoría, resultados)
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Runner)
            .WithMany(ru => ru.Results)
            .OnDelete(DeleteBehavior.Restrict);

        // Idempotency-Key opcional por carrera: si el cliente envía el header,
        // dos POSTs con el mismo key contra la misma carrera son la misma
        // captura. Índice filtrado para no chocar entre los miles de Results
        // legacy que tienen IdempotencyKey = NULL (Postgres y Sqlite ambos
        // respetan WHERE en índices únicos — mismo patrón que el de User.GoogleId).
        modelBuilder.Entity<Result>()
            .HasIndex(r => new { r.RaceId, r.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        modelBuilder.Entity<Result>()
            .Property(r => r.IdempotencyKey)
            .HasMaxLength(64);

        // Un corredor no puede tener dos resultados en la misma carrera.
        // ExistsByRunnerAsync (un simple AnyAsync) ya rechaza esto en el caso
        // común, pero no cierra la ventana de carrera entre dos capturas
        // casi simultáneas del mismo dorsal (típicamente sin Idempotency-Key).
        // Índice filtrado — mismo patrón que el de arriba — para no chocar
        // contra los Results sin corredor asignado todavía (RunnerId NULL).
        modelBuilder.Entity<Result>()
            .HasIndex(r => new { r.RaceId, r.RunnerId })
            .IsUnique()
            .HasFilter("\"RunnerId\" IS NOT NULL");

        modelBuilder.Entity<ResultAudit>()
            .HasOne(a => a.Result)
            .WithMany(r => r.AuditEntries)
            .OnDelete(DeleteBehavior.Cascade);

        // Bitácora transversal (Usuarios/Carreras/Categorías). Append-only.
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.EntityType).HasMaxLength(40).IsRequired();
            e.Property(a => a.Campo).HasMaxLength(60).IsRequired();
            e.Property(a => a.ValorAnterior).HasMaxLength(1024);
            e.Property(a => a.ValorNuevo).HasMaxLength(1024);

            // Índice que cubre el WHERE (EntityType, EntityId) y el ORDER BY CreatedAt DESC
            // de la consulta de historial → index range scan, sin sort en memoria.
            e.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt })
                .IsDescending(false, false, true)
                .HasDatabaseName("IX_AuditLog_Entity_Created");

            // Restrict para no perder historial si se intenta borrar al autor.
            e.HasOne(a => a.Autor)
                .WithMany()
                .HasForeignKey(a => a.AutorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

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

        // El barrido de reintentos (ProcessPendingAsync) filtra por Status en
        // cada corrida del cron — índice para no escanear toda la tabla.
        modelBuilder.Entity<NotificationLog>()
            .HasIndex(n => n.Status);

        // Enlace público de corredor (design.md Decisión 1-2): clave opaca de 22
        // caracteres Base64Url, única, nullable mientras dura la ventana de backfill.
        // Índice único filtrado — mismo patrón que IX_Users_Username — para no chocar
        // contra filas creadas por una instancia vieja durante un deploy en curso.
        modelBuilder.Entity<Runner>()
            .Property(r => r.PublicShareKey)
            .HasMaxLength(32);

        modelBuilder.Entity<Runner>()
            .HasIndex(r => r.PublicShareKey)
            .IsUnique()
            .HasFilter("\"PublicShareKey\" IS NOT NULL")
            .HasDatabaseName("IX_Runners_PublicShareKey");

        modelBuilder.Entity<Race>()
            .Property(r => r.Slogan)
            .HasMaxLength(160);

        // Soporta GetPlacingCountsAsync (design.md Decisión 3): las cuatro cuentas de
        // posicionamiento del enlace público resuelven en un solo GroupBy sobre este
        // índice, sin materializar los resultados de la carrera completa.
        modelBuilder.Entity<Result>()
            .HasIndex(r => new { r.RaceId, r.CategoryId, r.TiempoLlegada })
            .HasDatabaseName("IX_Results_RaceId_CategoryId_TiempoLlegada");

        // Controversias (backoffice-redesign-audit D1): listado por carrera + filtro estado.
        modelBuilder.Entity<TimingDispute>(e =>
        {
            e.Property(d => d.CorredorNombre).HasMaxLength(200).IsRequired();
            e.Property(d => d.Dorsal).HasMaxLength(32);
            e.Property(d => d.CapturistaNombre).HasMaxLength(120);
            e.Property(d => d.CapturaNote).HasMaxLength(500);

            e.HasIndex(d => new { d.RaceId, d.Estado })
                .HasDatabaseName("IX_TimingDisputes_RaceId_Estado");

            e.HasOne(d => d.Race)
                .WithMany()
                .HasForeignKey(d => d.RaceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.Result)
                .WithMany()
                .HasForeignKey(d => d.ResultId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(d => d.Runner)
                .WithMany()
                .HasForeignKey(d => d.RunnerId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(d => d.ResolvedBy)
                .WithMany()
                .HasForeignKey(d => d.ResolvedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Reconciliación del modelo EF con el estado real de Postgres: las migraciones
        // FixPostgres* de 2026-06/07 corrigieron estas columnas con SQL crudo directamente
        // sobre la base, sin nunca tocar el snapshot del modelo. Sin esta configuración
        // explícita, la primera migración generada contra Postgres real intenta revertir
        // esos cuatro arreglos en TODA la base (~1100 líneas de AlterColumn detectadas al
        // intentarlo). Este bloque no cambia nada en la BD: solo hace que el modelo C#
        // coincida con lo que Postgres ya tiene.

        // Identity: FixPostgresIdentityColumns pasó estas 9 PKs a
        // "generated by default as identity".
        modelBuilder.Entity<User>().Property(e => e.Id).UseIdentityByDefaultColumn();
        modelBuilder.Entity<Race>().Property(e => e.Id).UseIdentityByDefaultColumn();
        modelBuilder.Entity<RaceCategory>().Property(e => e.Id).UseIdentityByDefaultColumn();
        modelBuilder.Entity<Runner>().Property(e => e.Id).UseIdentityByDefaultColumn();
        modelBuilder.Entity<Result>().Property(e => e.Id).UseIdentityByDefaultColumn();
        modelBuilder.Entity<ResultAudit>().Property(e => e.Id).UseIdentityByDefaultColumn();
        modelBuilder.Entity<PublicResultToken>().Property(e => e.Id).UseIdentityByDefaultColumn();
        modelBuilder.Entity<NotificationLog>().Property(e => e.Id).UseIdentityByDefaultColumn();
        modelBuilder.Entity<RaceJudge>().Property(e => e.Id).UseIdentityByDefaultColumn();

        // Boolean: FixPostgresBooleanColumns pasó estas columnas de "integer" (0/1,
        // herencia de Sqlite) a "boolean".
        modelBuilder.Entity<User>().Property(e => e.IsActive).HasColumnType("boolean");
        modelBuilder.Entity<PublicResultToken>().Property(e => e.IsExpired).HasColumnType("boolean");

        // DateTime: FixPostgresDateTimeColumns pasó estas columnas de "text" (herencia
        // de Sqlite) a "timestamp without time zone" — la convención real de esta base,
        // no "timestamp with time zone". UtcDateTimeValueConverter (arriba) hace que
        // Npgsql acepte los DateTime.UtcNow (Kind=Utc) que el código siempre produce.
        modelBuilder.Entity<User>().Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<Race>().Property(e => e.FechaCarrera).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<Race>().Property(e => e.RaceStartUtc).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<Race>().Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<Race>().Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<RaceJudge>().Property(e => e.JoinedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<Runner>().Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<Result>().Property(e => e.TiempoLlegada).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<Result>().Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<Result>().Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<ResultAudit>().Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<PublicResultToken>().Property(e => e.FechaExpiracion).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<PublicResultToken>().Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<NotificationLog>().Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<NotificationLog>().Property(e => e.SentAt).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<RaceCategory>().Property(rc => rc.StartUtc).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);
        modelBuilder.Entity<RaceCategory>().Property(rc => rc.ClosedUtc).HasColumnType("timestamp without time zone").HasConversion(UtcDateTimeValueConverter);

        // Decimal: FixPostgresDecimalColumns pasó Categories.Distancia de "text"
        // (herencia de Sqlite) a "numeric". El array hardcodeado de esa migración dice
        // tabla "RaceCategories", pero eso es incorrecto — RaceCategories no tiene columna
        // Distancia; la columna real vive en Categories (verificado con psql \d).
        modelBuilder.Entity<Category>().Property(c => c.Distancia).HasColumnType("numeric");

        base.OnModelCreating(modelBuilder);
    }
}
