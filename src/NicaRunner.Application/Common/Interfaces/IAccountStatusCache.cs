namespace NicaRunner.Application.Common.Interfaces;

// backoffice-user-status-toggle PR3 (design.md D1/D2) -- abstracción de Application sobre
// el cache de estado "activo", consultada en cada request desde AccountStatusJwtEvents.
// La implementación vive en Infrastructure (Application no referencia Caching).
public interface IAccountStatusCache
{
    // Un miss recurre a GetByIdAsync y guarda el resultado con TTL.
    Task<bool> IsActiveAsync(int userId, CancellationToken ct = default);

    // Debe llamarse DESPUÉS de persistir (spec.md "Explicit invalidation on deactivation").
    void Invalidate(int userId);
}
