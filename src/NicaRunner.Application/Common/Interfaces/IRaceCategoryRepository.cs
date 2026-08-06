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

    /// <summary>Todas las asociaciones de la carrera, con Category y jueces cargados.</summary>
    /// <remarks>
    /// Entidades trackeadas por el DbContext, a propósito: si vas a mutarlas y guardar,
    /// reusalas en vez de volver a pedir la lista — EF te va a devolver las mismas
    /// instancias. Si solo necesitás leer, tratalas como si no lo estuvieran igual.
    /// </remarks>
    Task<List<RaceCategory>> GetAssociationsByRaceAsync(int raceId, CancellationToken ct = default);

    /// <summary>Solo las asociaciones de los CategoryId pedidos, en la misma carrera.</summary>
    /// <remarks>Trackeadas por el DbContext — ver <see cref="GetAssociationsByRaceAsync"/>.</remarks>
    Task<List<RaceCategory>> GetAssociationsByIdsAsync(
        int raceId, IReadOnlyCollection<int> categoryIds, CancellationToken ct = default);

    void Remove(RaceCategory association);
    Task SaveChangesAsync(CancellationToken ct = default);
}
