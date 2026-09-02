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
    /// Salida en falso: devuelve a Planeada las categorías EnCurso indicadas, borra su cero
    /// y anula las llegadas que se midieron contra él. Admin-only.
    ///
    /// No es `reopen` ni `correct-start`: reopen arregla un CIERRE equivocado y jamás toca
    /// StartUtc; correct-start le pone hora a una categoría que nunca arrancó. Esto es lo
    /// contrario de las dos — una categoría que SÍ arrancó y no debió hacerlo.
    /// </summary>
    Task<ResetStartResultDto> ResetStartAsync(
        int raceId, ResetCategoryStartRequest request, int actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Reinicio de la carrera COMPLETA por salida en falso: alcanza a todas sus categorías
    /// no-Planeada de una sola vez, incluidas las llegadas que todavía no tienen dorsal —
    /// si no queda ninguna categoría viva, no hay otra a la que pudieran pertenecer.
    /// La carrera vuelve a Planeada por derivación, sin RaceStartUtc. Admin-only.
    /// </summary>
    Task<ResetStartResultDto> RestartRaceAsync(
        int raceId, RestartRaceRequest request, int actorUserId, CancellationToken ct = default);

    /// <summary>
    /// PR 2b, motivo CategoriaSinSalida: corrige el StartUtc de una categoría Planeada
    /// que nunca arrancó a tiempo, la pasa a EnCurso, y dispara la cascada que revalida
    /// cualquier captura que había quedado en Controversia contra ella.
    /// </summary>
    Task<RaceCategoryDto> CorrectStartAsync(
        int raceId, int categoryId, CorrectCategoryStartRequest request, int actorUserId, CancellationToken ct = default);
}
