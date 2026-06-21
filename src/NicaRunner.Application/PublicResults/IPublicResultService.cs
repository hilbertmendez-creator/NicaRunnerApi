using NicaRunner.Application.PublicResults.Dtos;

namespace NicaRunner.Application.PublicResults;

public interface IPublicResultService
{
    Task<PublicTokenDto> CreateTokenAsync(int raceId, CreatePublicTokenRequest request, int creatorId, CancellationToken ct = default);
    Task<List<PublicTokenDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<PublicRaceResultsDto> GetResultsByTokenAsync(string token, CancellationToken ct = default);
    Task<PublicRunnerDetailDto> GetRunnerResultByTokenAsync(string token, int runnerId, CancellationToken ct = default);
}
