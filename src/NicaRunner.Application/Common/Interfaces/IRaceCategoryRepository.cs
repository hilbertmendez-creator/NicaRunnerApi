using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

// Categorías del catálogo global seleccionadas para una carrera.
public interface IRaceCategoryRepository
{
    Task<Category?> GetByIdAsync(int raceId, int categoryId, CancellationToken ct = default);
    Task<List<Category>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<bool> IsSelectedAsync(int raceId, int categoryId, CancellationToken ct = default);
    Task SelectAsync(int raceId, int categoryId, CancellationToken ct = default);
    Task<RaceCategory?> GetAssociationAsync(int raceId, int categoryId, CancellationToken ct = default);
    void Remove(RaceCategory association);
    Task SaveChangesAsync(CancellationToken ct = default);
}
