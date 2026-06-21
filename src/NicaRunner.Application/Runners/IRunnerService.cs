using NicaRunner.Application.Runners.Dtos;

namespace NicaRunner.Application.Runners;

public interface IRunnerService
{
    Task<RunnerDto> CreateAsync(int raceId, CreateRunnerRequest request, CancellationToken ct = default);
    Task<List<RunnerDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<RunnerDto> GetByIdAsync(int raceId, int runnerId, CancellationToken ct = default);
    Task<RunnerDto> UpdateAsync(int raceId, int runnerId, UpdateRunnerRequest request, CancellationToken ct = default);
    Task DeleteAsync(int raceId, int runnerId, CancellationToken ct = default);
    Task<ImportRunnersResultDto> ImportFromExcelAsync(int raceId, Stream excelStream, CancellationToken ct = default);
}
