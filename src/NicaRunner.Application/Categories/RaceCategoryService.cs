using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Categories;

public class RaceCategoryService(
    IRaceCategoryRepository categoryRepository,
    IRaceRepository raceRepository) : IRaceCategoryService
{
    public async Task<RaceCategoryDto> CreateAsync(int raceId, CreateRaceCategoryRequest request, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var category = new RaceCategory
        {
            RaceId = raceId,
            NombreCategoria = request.NombreCategoria,
            Distancia = request.Distancia,
            EdadMinima = request.EdadMinima,
            EdadMaxima = request.EdadMaxima,
            Orden = request.Orden
        };

        await categoryRepository.AddAsync(category, ct);
        await categoryRepository.SaveChangesAsync(ct);

        return ToDto(category);
    }

    public async Task<List<RaceCategoryDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var categories = await categoryRepository.GetAllByRaceAsync(raceId, ct);
        return categories.Select(ToDto).ToList();
    }

    public async Task<RaceCategoryDto> UpdateAsync(int raceId, int categoryId, UpdateRaceCategoryRequest request, CancellationToken ct = default)
    {
        var category = await GetCategoryOrThrowAsync(raceId, categoryId, ct);

        category.NombreCategoria = request.NombreCategoria;
        category.Distancia = request.Distancia;
        category.EdadMinima = request.EdadMinima;
        category.EdadMaxima = request.EdadMaxima;
        category.Orden = request.Orden;

        await categoryRepository.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task DeleteAsync(int raceId, int categoryId, CancellationToken ct = default)
    {
        var category = await GetCategoryOrThrowAsync(raceId, categoryId, ct);
        categoryRepository.Remove(category);
        await categoryRepository.SaveChangesAsync(ct);
    }

    private async Task EnsureRaceExistsAsync(int raceId, CancellationToken ct)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");
    }

    private async Task<RaceCategory> GetCategoryOrThrowAsync(int raceId, int categoryId, CancellationToken ct) =>
        await categoryRepository.GetByIdAsync(raceId, categoryId, ct)
            ?? throw new NotFoundException($"No existe la categoría con id {categoryId} en la carrera {raceId}.");

    private static RaceCategoryDto ToDto(RaceCategory category) => new(
        category.Id,
        category.RaceId,
        category.NombreCategoria,
        category.Distancia,
        category.EdadMinima,
        category.EdadMaxima,
        category.Orden);
}
