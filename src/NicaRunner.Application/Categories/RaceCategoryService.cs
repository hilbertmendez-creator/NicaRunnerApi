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

    public async Task<List<RaceCategoryDto>> StartAsync(
        int raceId, CategoryTransitionRequest request, int actorUserId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        var targets = await ResolveTargetsOrThrowAsync(raceId, request, ct);

        var yaArrancadas = targets.Where(t => t.Estado != RaceCategoryStatus.Planeada).ToList();
        if (yaArrancadas.Count > 0)
            throw new ConflictException(
                "Estas categorías ya no están Planeada: " +
                string.Join(", ", yaArrancadas.Select(t => t.Category.NombreCategoria)) + ".");

        // UNA sola lectura del reloj para las N categorías. Esto es el punto entero
        // del endpoint: salieron con el mismo disparo, arrancan con el mismo cero.
        var startUtc = DateTime.UtcNow;

        foreach (var target in targets)
        {
            target.Estado = RaceCategoryStatus.EnCurso;
            target.StartUtc = startUtc;
            target.StartedByUserId = actorUserId;
        }

        await SyncRaceStateAsync(race, ct);
        await raceCategoryRepository.SaveChangesAsync(ct);

        return targets.Select(ToDto).ToList();
    }

    public async Task<List<RaceCategoryDto>> CloseAsync(
        int raceId, CategoryTransitionRequest request, int actorUserId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        var targets = await ResolveTargetsOrThrowAsync(raceId, request, ct);

        var noEnCurso = targets.Where(t => t.Estado != RaceCategoryStatus.EnCurso).ToList();
        if (noEnCurso.Count > 0)
            throw new ConflictException(
                "Solo se pueden cerrar categorías EnCurso. Estas no lo están: " +
                string.Join(", ", noEnCurso.Select(t => t.Category.NombreCategoria)) + ".");

        var closedUtc = DateTime.UtcNow;

        foreach (var target in targets)
        {
            target.Estado = RaceCategoryStatus.Terminada;
            target.ClosedUtc = closedUtc;
            target.ClosedByUserId = actorUserId;
        }

        await SyncRaceStateAsync(race, ct);
        await raceCategoryRepository.SaveChangesAsync(ct);

        return targets.Select(ToDto).ToList();
    }

    public async Task<List<RaceCategoryDto>> ReopenAsync(
        int raceId, CategoryTransitionRequest request, int actorUserId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        var targets = await ResolveTargetsOrThrowAsync(raceId, request, ct);

        var noTerminadas = targets.Where(t => t.Estado != RaceCategoryStatus.Terminada).ToList();
        if (noTerminadas.Count > 0)
            throw new ConflictException(
                "Solo se puede reabrir una categoría Terminada. Estas no lo están: " +
                string.Join(", ", noTerminadas.Select(t => t.Category.NombreCategoria)) + ".");

        foreach (var target in targets)
        {
            // StartUtc NO se toca: reabrir corrige un cierre equivocado, no vuelve a
            // disparar la salida. El cero de la categoría sigue siendo el mismo.
            target.Estado = RaceCategoryStatus.EnCurso;
            target.ClosedUtc = null;
            target.ClosedByUserId = null;
        }

        await SyncRaceStateAsync(race, ct);
        await raceCategoryRepository.SaveChangesAsync(ct);

        return targets.Select(ToDto).ToList();
    }

    private async Task<List<RaceCategory>> ResolveTargetsOrThrowAsync(
        int raceId, CategoryTransitionRequest request, CancellationToken ct)
    {
        var ids = request.CategoryIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new ValidationException("Seleccioná al menos una categoría.");

        var targets = await raceCategoryRepository.GetAssociationsByIdsAsync(raceId, ids, ct);

        var faltantes = ids.Except(targets.Select(t => t.CategoryId)).ToList();
        if (faltantes.Count > 0)
            throw new NotFoundException(
                $"Estas categorías no están asignadas a la carrera {raceId}: {string.Join(", ", faltantes)}.");

        return targets;
    }

    /// <summary>
    /// Race.Estado y Race.RaceStartUtc son derivados: se recalculan acá, en la misma
    /// transacción que la transición que los provocó. Nunca se setean desde afuera.
    /// </summary>
    private async Task SyncRaceStateAsync(Race race, CancellationToken ct)
    {
        var all = await raceCategoryRepository.GetAssociationsByRaceAsync(race.Id, ct);

        race.Estado = RaceStatusDeriver.From(all);

        // Backcompat: RaceStartUtc pasa a ser el primer arranque de cualquier categoría.
        // Ya no se calcula elapsed a partir de él, pero el móvil actual todavía lo lee.
        var arranques = all.Where(c => c.StartUtc is not null).Select(c => c.StartUtc!.Value).ToList();
        race.RaceStartUtc = arranques.Count == 0 ? null : arranques.Min();

        race.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<Race> GetRaceOrThrowAsync(int raceId, CancellationToken ct) =>
        await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

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

    private static RaceCategoryDto ToDto(RaceCategory rc) => new(
        rc.Category.Id,
        rc.Category.Codigo,
        rc.Category.NombreCategoria,
        rc.Category.Descripcion,
        rc.Category.Distancia,
        rc.Category.EdadMinima,
        rc.Category.EdadMaxima,
        rc.Category.Orden,
        rc.Estado,
        rc.StartUtc,
        rc.StartedByUserId,
        rc.StartedBy?.Nombre,
        rc.ClosedUtc,
        rc.ClosedByUserId,
        rc.ClosedBy?.Nombre);
}
