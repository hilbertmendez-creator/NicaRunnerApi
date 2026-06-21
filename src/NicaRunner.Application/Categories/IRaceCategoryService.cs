using NicaRunner.Application.Categories.Dtos;

namespace NicaRunner.Application.Categories;

public interface IRaceCategoryService
{
    Task<RaceCategoryDto> CreateAsync(int raceId, CreateRaceCategoryRequest request, CancellationToken ct = default);
    Task<List<RaceCategoryDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<RaceCategoryDto> UpdateAsync(int raceId, int categoryId, UpdateRaceCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(int raceId, int categoryId, CancellationToken ct = default);
}
