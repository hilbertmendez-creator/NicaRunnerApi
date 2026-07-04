using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int categoryId, CancellationToken ct = default);
    Task<List<Category>> GetAllAsync(CancellationToken ct = default);
    Task<bool> CodigoExistsAsync(string codigo, int? excludeCategoryId = null, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    void Remove(Category category);
    Task SaveChangesAsync(CancellationToken ct = default);
}
