using NicaRunner.Application.Categories.Dtos;

namespace NicaRunner.Application.Categories;

public interface IRaceCategoryService
{
    Task<RaceCategoryDto> AssignAsync(int raceId, AssignCategoryRequest request, CancellationToken ct = default);
    Task<List<RaceCategoryDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task UnassignAsync(int raceId, int categoryId, CancellationToken ct = default);

    Task<List<RaceCategoryDto>> StartAsync(
        int raceId, CategoryTransitionRequest request, int actorUserId, CancellationToken ct = default);

    Task<List<RaceCategoryDto>> CloseAsync(
        int raceId, CategoryTransitionRequest request, int actorUserId, CancellationToken ct = default);

    Task<List<RaceCategoryDto>> ReopenAsync(
        int raceId, CategoryTransitionRequest request, int actorUserId, CancellationToken ct = default);

    /// <summary>
    /// PR 2b, motivo CategoriaSinSalida: corrige el StartUtc de una categoría Planeada
    /// que nunca arrancó a tiempo, la pasa a EnCurso, y dispara la cascada que revalida
    /// cualquier captura que había quedado en Controversia contra ella.
    /// </summary>
    Task<RaceCategoryDto> CorrectStartAsync(
        int raceId, int categoryId, CorrectCategoryStartRequest request, int actorUserId, CancellationToken ct = default);
}
