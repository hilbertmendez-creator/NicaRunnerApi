using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Categories;

public class CategoryService(
    ICategoryRepository categoryRepository,
    IRunnerRepository runnerRepository,
    IAuditService auditService) : ICategoryService
{
    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        EnsureValidAgeRange(request.EdadMinima, request.EdadMaxima);
        await EnsureCodigoIsUniqueAsync(request.Codigo, null, ct);

        var category = new Category
        {
            Codigo = request.Codigo.Trim().ToUpperInvariant(),
            NombreCategoria = request.NombreCategoria,
            Descripcion = request.Descripcion,
            Distancia = request.Distancia,
            EdadMinima = request.EdadMinima,
            EdadMaxima = request.EdadMaxima,
            Orden = request.Orden
        };

        await categoryRepository.AddAsync(category, ct);
        await categoryRepository.SaveChangesAsync(ct);

        return ToDto(category);
    }

    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await categoryRepository.GetAllAsync(ct);
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(int categoryId, CancellationToken ct = default) =>
        ToDto(await GetCategoryOrThrowAsync(categoryId, ct));

    public async Task<CategoryDto> UpdateAsync(int categoryId, UpdateCategoryRequest request, int currentUserId, CancellationToken ct = default)
    {
        var category = await GetCategoryOrThrowAsync(categoryId, ct);
        EnsureValidAgeRange(request.EdadMinima, request.EdadMaxima);
        await EnsureCodigoIsUniqueAsync(request.Codigo, categoryId, ct);

        var nuevoCodigo = request.Codigo.Trim().ToUpperInvariant();
        var changes = new List<FieldChange>
        {
            new("Codigo", category.Codigo, nuevoCodigo),
            new("NombreCategoria", category.NombreCategoria, request.NombreCategoria),
            new("Descripcion", AuditValue.Of(category.Descripcion), AuditValue.Of(request.Descripcion)),
            new("Distancia", AuditValue.Of(category.Distancia), AuditValue.Of(request.Distancia)),
            new("EdadMinima", AuditValue.Of(category.EdadMinima), AuditValue.Of(request.EdadMinima)),
            new("EdadMaxima", AuditValue.Of(category.EdadMaxima), AuditValue.Of(request.EdadMaxima)),
            new("Orden", AuditValue.Of(category.Orden), AuditValue.Of(request.Orden)),
        };

        category.Codigo = nuevoCodigo;
        category.NombreCategoria = request.NombreCategoria;
        category.Descripcion = request.Descripcion;
        category.Distancia = request.Distancia;
        category.EdadMinima = request.EdadMinima;
        category.EdadMaxima = request.EdadMaxima;
        category.Orden = request.Orden;

        auditService.TrackChanges(AuditEntityTypes.Category, category.Id, currentUserId, changes);

        await categoryRepository.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task DeleteAsync(int categoryId, CancellationToken ct = default)
    {
        var category = await GetCategoryOrThrowAsync(categoryId, ct);

        if (await runnerRepository.ExistsByCategoryAsync(categoryId, ct))
            throw new ConflictException($"No se puede eliminar la categoría '{category.NombreCategoria}': tiene corredores inscritos.");

        categoryRepository.Remove(category);
        await categoryRepository.SaveChangesAsync(ct);
    }

    private static void EnsureValidAgeRange(int edadMinima, int edadMaxima)
    {
        if (edadMinima >= edadMaxima)
            throw new ValidationException("La edad mínima debe ser menor que la edad máxima.");
    }

    private async Task EnsureCodigoIsUniqueAsync(string codigo, int? excludeCategoryId, CancellationToken ct)
    {
        if (await categoryRepository.CodigoExistsAsync(codigo.Trim(), excludeCategoryId, ct))
            throw new ConflictException($"Ya existe una categoría con el código '{codigo}'.");
    }

    private async Task<Category> GetCategoryOrThrowAsync(int categoryId, CancellationToken ct) =>
        await categoryRepository.GetByIdAsync(categoryId, ct)
            ?? throw new NotFoundException($"No existe la categoría con id {categoryId}.");

    private static CategoryDto ToDto(Category category) => new(
        category.Id,
        category.Codigo,
        category.NombreCategoria,
        category.Descripcion,
        category.Distancia,
        category.EdadMinima,
        category.EdadMaxima,
        category.Orden);
}
