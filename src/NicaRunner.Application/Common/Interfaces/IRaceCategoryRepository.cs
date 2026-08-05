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

    // public-runner-registration-manual-payment: a diferencia de los métodos de arriba
    // (que reciben Category.Id), estos dos buscan por el Id propio de la fila RaceCategory
    // — es la clave que Registration.RaceCategoryId referencia (design.md D1), y la que
    // trae Capacidad/Precio/ConfirmedCount para el flujo de inscripción pública/confirm.
    Task<RaceCategory?> GetRaceCategoryByIdAsync(int raceId, int raceCategoryId, CancellationToken ct = default);
    Task<List<RaceCategory>> GetAllRaceCategoriesByRaceAsync(int raceId, CancellationToken ct = default);
}
