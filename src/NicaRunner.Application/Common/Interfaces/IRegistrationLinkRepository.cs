using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

// design.md D4: RegistrationLink espeja PublicResultToken — mismo patrón de repositorio
// (GetByTokenAsync + AddAsync + SaveChangesAsync). GetByIdAsync/GetAllByRaceAsync se
// agregaron en tasks.md 2.12 para la administración (crear/revocar) que faltaba.
public interface IRegistrationLinkRepository
{
    Task<RegistrationLink?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<RegistrationLink?> GetByIdAsync(int raceId, int linkId, CancellationToken ct = default);
    Task<List<RegistrationLink>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task AddAsync(RegistrationLink link, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
