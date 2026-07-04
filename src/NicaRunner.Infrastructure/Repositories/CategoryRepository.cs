using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class CategoryRepository(NicaRunnerDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(int categoryId, CancellationToken ct = default) =>
        context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);

    public Task<List<Category>> GetAllAsync(CancellationToken ct = default) =>
        context.Categories.OrderBy(c => c.Orden).ToListAsync(ct);

    public Task<bool> CodigoExistsAsync(string codigo, int? excludeCategoryId = null, CancellationToken ct = default) =>
        context.Categories.AnyAsync(
            c => c.Codigo.ToUpper() == codigo.ToUpper() && c.Id != excludeCategoryId,
            ct);

    public async Task AddAsync(Category category, CancellationToken ct = default) =>
        await context.Categories.AddAsync(category, ct);

    public void Remove(Category category) => context.Categories.Remove(category);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
