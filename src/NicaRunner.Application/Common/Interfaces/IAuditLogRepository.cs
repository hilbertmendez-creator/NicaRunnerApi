using NicaRunner.Application.Auditing.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    /// <summary>
    /// Encola las filas en el ChangeTracker. NO persiste: el commit lo hace el
    /// SaveChangesAsync del servicio dueño de la transacción (misma transacción,
    /// cero round-trips extra).
    /// </summary>
    void AddRange(IEnumerable<AuditLog> entries);

    /// <summary>
    /// Historial de un registro, más reciente primero, proyectado a DTO con el
    /// nombre del autor (JOIN en una sola query), sin tracking. Paginación keyset
    /// opcional vía <paramref name="beforeUtc"/>.
    /// </summary>
    Task<List<AuditLogDto>> GetHistoryAsync(
        string entityType, int entityId, int limit, DateTime? beforeUtc, CancellationToken ct = default);
}
