using NicaRunner.Application.Auditing.Dtos;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auditing;

public class AuditService(IAuditLogRepository repository) : IAuditService
{
    // Debe coincidir con el HasMaxLength de la columna en NicaRunnerDbContext.
    private const int MaxValueLength = 1024;

    public void TrackChanges(string entityType, int entityId, int autorId, IEnumerable<FieldChange> changes)
    {
        var rows = changes
            .Where(c => c.ValorAnterior != c.ValorNuevo)   // solo-si-cambió: un no-cambio = 0 filas, 0 I/O
            .Select(c => new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Campo = c.Campo,
                ValorAnterior = Truncate(c.ValorAnterior),
                ValorNuevo = Truncate(c.ValorNuevo),
                AutorId = autorId,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (rows.Count > 0)
            repository.AddRange(rows);
    }

    public Task<List<AuditLogDto>> GetHistoryAsync(
        string entityType, int entityId, int limit = 50, DateTime? beforeUtc = null, CancellationToken ct = default)
        => repository.GetHistoryAsync(entityType, entityId, limit, beforeUtc, ct);

    // Recorte defensivo: la representación textual nunca debe exceder el ancho de columna.
    private static string? Truncate(string? value) =>
        value is { Length: > MaxValueLength } ? value[..MaxValueLength] : value;
}
