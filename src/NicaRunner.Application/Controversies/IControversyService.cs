using NicaRunner.Application.Controversies.Dtos;

namespace NicaRunner.Application.Controversies;

public interface IControversyService
{
    Task<List<ControversyDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<ControversyDto> ResolveAsync(int raceId, int controversyId, ResolveControversyRequest request, CancellationToken ct = default);
    Task<ControversySummaryDto> GetSummaryAsync(int raceId, CancellationToken ct = default);
}