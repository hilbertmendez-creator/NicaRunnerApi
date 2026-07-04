using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Dashboard;
using NicaRunner.Application.Dashboard.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// Dashboard en vivo y standings: reservado a Admin/Lector por spec (sección 6,
// "Dashboard (Admin & Lector)"). Capturista no lo necesita para capturar.
[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}")]
[Route("api/v{version:apiVersion}/races/{raceId:int}")]
[Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Lector)}")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<RaceDashboardDto>> GetDashboard(int raceId, CancellationToken ct) =>
        Ok(await dashboardService.GetDashboardAsync(raceId, ct));

    [HttpGet("standings")]
    public async Task<ActionResult<List<CategoryStandingsDto>>> GetStandings(int raceId, CancellationToken ct) =>
        Ok(await dashboardService.GetStandingsAsync(raceId, ct));
}
