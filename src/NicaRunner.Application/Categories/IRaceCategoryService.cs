using NicaRunner.Application.Categories.Dtos;

namespace NicaRunner.Application.Categories;

public interface IRaceCategoryService
{
    Task<RaceCategoryDto> AssignAsync(int raceId, AssignCategoryRequest request, CancellationToken ct = default);
    Task<List<RaceCategoryDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task UnassignAsync(int raceId, int categoryId, CancellationToken ct = default);

    // registration-review spec.md "RaceCategory Capacity and Price Configuration"
    // (tasks.md 2.11): configures Capacidad/Precio on an already-assigned RaceCategory.
    Task<RaceCategoryDto> ConfigureAsync(int raceId, int categoryId, ConfigureRaceCategoryRequest request, CancellationToken ct = default);
}
