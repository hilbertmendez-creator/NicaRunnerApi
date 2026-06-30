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
    Task<List<ResultDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<ResultDto> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default);
    Task<ResultDto> UpdateAsync(int raceId, int resultId, UpdateResultRequest request, int editorId, CancellationToken ct = default);
    Task<List<ResultAuditDto>> GetAuditAsync(int raceId, int resultId, CancellationToken ct = default);
}
