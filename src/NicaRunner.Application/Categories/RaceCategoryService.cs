using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Categories;

public class RaceCategoryService(
    IRaceCategoryRepository raceCategoryRepository,
    ICategoryRepository categoryRepository,
    IRaceRepository raceRepository,
    IRunnerRepository runnerRepository) : IRaceCategoryService
{
    public async Task<RaceCategoryDto> AssignAsync(int raceId, AssignCategoryRequest request, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException($"No existe la categoría con id {request.CategoryId} en el catálogo.");

        if (await raceCategoryRepository.IsSelectedAsync(raceId, request.CategoryId, ct))
            throw new ConflictException($"La categoría '{category.NombreCategoria}' ya está asignada a esta carrera.");

        await raceCategoryRepository.SelectAsync(raceId, request.CategoryId, ct);
        await raceCategoryRepository.SaveChangesAsync(ct);

        return ToDto(category);
    }

    public async Task<List<RaceCategoryDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var categories = await raceCategoryRepository.GetAllByRaceAsync(raceId, ct);
        return categories.Select(ToDto).ToList();
    }

    public async Task UnassignAsync(int raceId, int categoryId, CancellationToken ct = default)
    {
        var association = await raceCategoryRepository.GetAssociationAsync(raceId, categoryId, ct)
            ?? throw new NotFoundException($"La categoría con id {categoryId} no está asignada a la carrera {raceId}.");

        if (await runnerRepository.ExistsByCategoryInRaceAsync(raceId, categoryId, ct))
            throw new ConflictException("No se puede quitar esta categoría: ya tiene corredores inscritos en esta carrera.");

        raceCategoryRepository.Remove(association);
        await raceCategoryRepository.SaveChangesAsync(ct);
    }

    // registration-review spec.md "RaceCategory Capacity and Price Configuration"
    // (tasks.md 2.11): Capacidad stays whatever the admin supplies, including null
    // (design.md D1: null = sin límite/unlimited, a legitimate final state — never
    // forced non-null here). Precio is required by ConfigureRaceCategoryRequest's own
    // validation; it is the field RegistrationService.CreateLinkAsync checks before
    // allowing a RegistrationLink to be opened for the race.
    public async Task<RaceCategoryDto> ConfigureAsync(int raceId, int categoryId, ConfigureRaceCategoryRequest request, CancellationToken ct = default)
    {
        var association = await raceCategoryRepository.GetAssociationAsync(raceId, categoryId, ct)
            ?? throw new NotFoundException($"La categoría con id {categoryId} no está asignada a la carrera {raceId}.");

        association.Capacidad = request.Capacidad;
        association.Precio = request.Precio;

        await raceCategoryRepository.SaveChangesAsync(ct);

        var category = await categoryRepository.GetByIdAsync(categoryId, ct)
            ?? throw new NotFoundException($"No existe la categoría con id {categoryId} en el catálogo.");

        return ToDto(category, association);
    }

    private async Task EnsureRaceExistsAsync(int raceId, CancellationToken ct)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");
    }

    private static RaceCategoryDto ToDto(Category category) => new(
        category.Id,
        category.Codigo,
        category.NombreCategoria,
        category.Descripcion,
        category.Distancia,
        category.EdadMinima,
        category.EdadMaxima,
        category.Orden);

    private static RaceCategoryDto ToDto(Category category, RaceCategory association) => new(
        category.Id,
        category.Codigo,
        category.NombreCategoria,
        category.Descripcion,
        category.Distancia,
        category.EdadMinima,
        category.EdadMaxima,
        category.Orden,
        association.Capacidad,
        association.Precio,
        association.ConfirmedCount);
}
