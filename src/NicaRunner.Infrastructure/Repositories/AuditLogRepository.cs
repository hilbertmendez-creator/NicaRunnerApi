using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Auditing.Dtos;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class AuditLogRepository(NicaRunnerDbContext context) : IAuditLogRepository
{
    // No persiste: solo marca en el ChangeTracker. El SaveChangesAsync del servicio
    // dueño commitea el cambio de la entidad y estas filas en la misma transacción.
    public void AddRange(IEnumerable<AuditLog> entries) => context.AuditLogs.AddRange(entries);

    public async Task<List<AuditLogDto>> GetHistoryAsync(
        string entityType, int entityId, int limit, DateTime? beforeUtc, CancellationToken ct = default)
    {
        // Usa IX_AuditLog_Entity_Created: el índice sirve el WHERE y el ORDER BY DESC
        // sin sort en memoria; la proyección con Autor.Nombre resuelve el nombre del
        // autor en una sola query (sin N+1) y AsNoTracking evita el change-tracker.
        var query = context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId);

        if (beforeUtc is { } before)
            query = query.Where(a => a.CreatedAt < before); // paginación keyset (sin OFFSET)

        var rows = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(a => new AuditLogDto(
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Campo,
                a.ValorAnterior,
                a.ValorNuevo,
                a.AutorId,
                a.Autor.Nombre,
                a.CreatedAt))
            .ToListAsync(ct);

        // Sqlite/Postgres devuelven CreatedAt con Kind=Unspecified (no preservan el
        // Kind), y System.Text.Json serializa eso sin sufijo "Z" — el navegador lo
        // interpretaría como hora LOCAL en vez de UTC (AC-8 rompería en 6h en NI).
        // Se corrige acá, acotado a esta bitácora, sin tocar serialización global.
        return rows.ConvertAll(r => r with { CreatedAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc) });
    }
}
