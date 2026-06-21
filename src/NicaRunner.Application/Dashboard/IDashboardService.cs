using NicaRunner.Application.Dashboard.Dtos;

namespace NicaRunner.Application.Dashboard;

public interface IDashboardService
{
    Task<RaceDashboardDto> GetDashboardAsync(int raceId, CancellationToken ct = default);
    Task<List<CategoryStandingsDto>> GetStandingsAsync(int raceId, CancellationToken ct = default);
}
