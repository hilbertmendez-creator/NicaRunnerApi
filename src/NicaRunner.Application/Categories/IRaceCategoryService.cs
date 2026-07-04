using NicaRunner.Application.Categories.Dtos;

namespace NicaRunner.Application.Categories;

public interface IRaceCategoryService
{
    Task<RaceCategoryDto> AssignAsync(int raceId, AssignCategoryRequest request, CancellationToken ct = default);
    Task<List<RaceCategoryDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task UnassignAsync(int raceId, int categoryId, CancellationToken ct = default);
}
