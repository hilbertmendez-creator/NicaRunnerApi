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
}
