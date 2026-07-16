using NicaRunner.Application.Auditing.Dtos;

namespace NicaRunner.Application.Auditing;

public interface IAuditService
{
    /// <summary>
    /// Encola en el ChangeTracker SOLO los campos que realmente cambiaron. No
    /// persiste: el SaveChangesAsync del servicio llamador commitea el cambio de
    /// la entidad y su auditoría en la misma transacción.
    /// </summary>
    void TrackChanges(string entityType, int entityId, int autorId, IEnumerable<FieldChange> changes);

    Task<List<AuditLogDto>> GetHistoryAsync(
        string entityType, int entityId, int limit = 50, DateTime? beforeUtc = null, CancellationToken ct = default);
}
