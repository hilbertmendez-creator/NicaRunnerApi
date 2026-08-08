using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Results.Dtos;

namespace NicaRunner.Application.Results;

public interface IResultService
{
    /// <summary>
    /// Crea un nuevo resultado. Si <paramref name="idempotencyKey"/> no es null
    /// y ya existe un Result en esta carrera con el mismo key, devuelve el
    /// existente sin crear uno nuevo (idempotente por carrera). Pensado para
    /// que la app móvil del capturista pueda reintentar el POST después de un
    /// timeout de red sin generar capturas duplicadas.
    /// </summary>
    Task<ResultDto> CreateAsync(int raceId, CreateResultRequest request, int capturistaId, string? idempotencyKey = null, CancellationToken ct = default);
    Task<PaginatedList<ResultDto>> GetAllByRaceAsync(int raceId, int limit = 50, int offset = 0, CancellationToken ct = default);
    Task<ResultDto> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default);
    Task<ResultDto> UpdateAsync(int raceId, int resultId, UpdateResultRequest request, int editorId, CancellationToken ct = default);
    Task<List<ResultAuditDto>> GetAuditAsync(int raceId, int resultId, CancellationToken ct = default);

    /// <summary>
    /// D5: el autor de la captura puede deshacer lo suyo mientras la carrera no esté
    /// Terminada; un Admin puede deshacer cualquier captura, siempre. Pasa el resultado
    /// a Estado=Anulado, resetea su Posicion a 0 y recalcula las posiciones de la
    /// categoría afectada.
    /// </summary>
    Task<ResultDto> VoidAsync(int raceId, int resultId, VoidResultRequest request, int actorUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// F5, acotado a DorsalDuplicado en este PR. Resuelve un grupo de disputa
    /// aplicando las asignaciones de dorsal indicadas y anulando el resto.
    /// </summary>
    Task<List<ResultDto>> ResolveDisputeAsync(int raceId, int disputeGroupId, ResolveDisputeGroupRequest request, int actorUserId, CancellationToken ct = default);

    /// <summary>Lista los grupos de disputa abiertos (Estado=Controversia) de la carrera.</summary>
    Task<List<DisputeGroupDto>> GetOpenDisputesAsync(int raceId, CancellationToken ct = default);

    /// <summary>
    /// PR 2b: reintenta cada resultado en Controversia de `categoryId` cuyo motivo sea
    /// CategoriaSinSalida o CategoriaCerrada, ahora que la categoría volvió a EnCurso
    /// (por CorrectStartAsync o por ReopenAsync). Devuelve cuántos volvieron a Valido.
    /// </summary>
    Task<int> ResolvePendingCategoryDisputesAsync(
        int raceId, int categoryId, int actorUserId, string razon, CancellationToken ct = default);
}
