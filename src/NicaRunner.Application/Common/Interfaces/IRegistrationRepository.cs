using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

// design.md D2/D3: TryClaimAsync/TryReserveSlotAsync/ReleaseSlotAsync/ReleaseClaimAsync
// son conditional UPDATEs atómicos (ExecuteUpdateAsync) — sin transacción ambiente, el
// rows-affected de cada uno ES el veredicto. RegistrationService orquesta el pipeline
// reserve-then-compensate: claim -> reserve -> promote(+AuditLog, un solo
// SaveChangesAsync) -> notify; cualquier falla entre reserve y promote revierte 2 y 1.
public interface IRegistrationRepository
{
    Task<Registration?> GetByIdAsync(int raceId, int registrationId, CancellationToken ct = default);
    Task<List<Registration>> GetAllByRaceAsync(int raceId, RegistrationStatus? estado, CancellationToken ct = default);
    Task AddAsync(Registration registration, CancellationToken ct = default);

    /// <summary>
    /// Paso 1 (claim): SET Estado=Confirmada, RevisadoPorUserId, RevisadoAt WHERE
    /// Id=@registrationId AND Estado=ComprobanteSubido. rows=0 → ya confirmada/rechazada
    /// por otra petición concurrente, o no está en el estado esperado — es el latch de
    /// idempotencia (design.md Data Flow, "Step 1 is the idempotency latch").
    /// </summary>
    Task<int> TryClaimAsync(int registrationId, int adminId, DateTime now, CancellationToken ct = default);

    /// <summary>
    /// Revierte el claim del paso 1 cuando el paso 2 (reserve) o 3 (promote) falla —
    /// vuelve a ComprobanteSubido y limpia los campos de revisión. Solo actúa si
    /// RunnerId sigue null (nunca revierte una confirmación ya promovida).
    /// </summary>
    Task<int> ReleaseClaimAsync(int registrationId, CancellationToken ct = default);

    /// <summary>
    /// Paso 2 (reserve): SET ConfirmedCount=ConfirmedCount+1 WHERE Id=@raceCategoryId AND
    /// (Capacidad IS NULL OR ConfirmedCount &lt; Capacidad). rows=0 → cupo lleno.
    /// </summary>
    Task<int> TryReserveSlotAsync(int raceCategoryId, CancellationToken ct = default);

    /// <summary>
    /// Compensa el paso 2 cuando el paso 3 (promote) falla después de reservar el cupo.
    /// </summary>
    Task<int> ReleaseSlotAsync(int raceCategoryId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
