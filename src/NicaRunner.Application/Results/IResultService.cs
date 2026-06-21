using NicaRunner.Application.Results.Dtos;

namespace NicaRunner.Application.Results;

public interface IResultService
{
    Task<ResultDto> CreateAsync(int raceId, CreateResultRequest request, int capturistaId, CancellationToken ct = default);
    Task<List<ResultDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<ResultDto> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default);
    Task<ResultDto> UpdateAsync(int raceId, int resultId, UpdateResultRequest request, int editorId, CancellationToken ct = default);
    Task<List<ResultAuditDto>> GetAuditAsync(int raceId, int resultId, CancellationToken ct = default);
}
