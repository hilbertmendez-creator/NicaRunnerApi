using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Dashboard;
using NicaRunner.Application.Dashboard.Dtos;

namespace NicaRunner.Api.Controllers;

[ApiController]
[Route("api/races/{raceId:int}")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<RaceDashboardDto>> GetDashboard(int raceId, CancellationToken ct) =>
        Ok(await dashboardService.GetDashboardAsync(raceId, ct));

    [HttpGet("standings")]
    public async Task<ActionResult<List<CategoryStandingsDto>>> GetStandings(int raceId, CancellationToken ct) =>
        Ok(await dashboardService.GetStandingsAsync(raceId, ct));
}
