using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

// design.md D8/D9: consultado en todo write path de Dorsal (confirm individual, confirm
// bulk, y el manual RunnerService.CreateAsync/UpdateAsync), siempre comparando sobre el
// valor normalizado (D11), nunca el string crudo. La implementación concreta
// (ReservedDorsalRepository) llega en PR2 — esta interfaz es el contrato de dominio.
public interface IReservedDorsalRepository
{
    Task<bool> IsReservedAsync(int raceId, string dorsal, CancellationToken ct = default);
    Task<List<ReservedDorsal>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<ReservedDorsal?> GetByIdAsync(int raceId, int reservedDorsalId, CancellationToken ct = default);
    Task AddAsync(ReservedDorsal reservedDorsal, CancellationToken ct = default);
    void Remove(ReservedDorsal reservedDorsal);
    Task SaveChangesAsync(CancellationToken ct = default);
}
