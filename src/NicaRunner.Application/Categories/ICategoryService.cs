using NicaRunner.Application.Categories.Dtos;

namespace NicaRunner.Application.Categories;

public interface ICategoryService
{
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<List<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> GetByIdAsync(int categoryId, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int categoryId, UpdateCategoryRequest request, int currentUserId, CancellationToken ct = default);
    Task DeleteAsync(int categoryId, CancellationToken ct = default);
}
