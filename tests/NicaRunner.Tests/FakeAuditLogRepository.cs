using NicaRunner.Application.Auditing.Dtos;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

/// <summary>
/// Fake en memoria de <see cref="IAuditLogRepository"/> para pruebas de los
/// servicios que auditan (Users/Races/Categories) sin mockear cada llamada a
/// AddRange — se verifica directamente el contenido de <see cref="Entries"/>.
/// </summary>
public class FakeAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> Entries { get; } = [];

    public void AddRange(IEnumerable<AuditLog> entries) => Entries.AddRange(entries);

    public Task<List<AuditLogDto>> GetHistoryAsync(
        string entityType, int entityId, int limit, DateTime? beforeUtc, CancellationToken ct = default)
    {
        var query = Entries.Where(a => a.EntityType == entityType && a.EntityId == entityId);
        if (beforeUtc is { } before)
            query = query.Where(a => a.CreatedAt < before);

        var result = query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AuditLogDto(
                a.Id, a.EntityType, a.EntityId, a.Campo, a.ValorAnterior, a.ValorNuevo,
                a.AutorId, "Autor de prueba", a.CreatedAt))
            .ToList();

        return Task.FromResult(result);
    }
}
