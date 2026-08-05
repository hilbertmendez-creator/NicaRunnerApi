using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Auditing.Dtos;
using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Races;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/races")]
[Route("api/v{version:apiVersion}/races")]
[Authorize]
public class RacesController(IRaceService raceService, IAuditService auditService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceDto>> Create(CreateRaceRequest request, CancellationToken ct)
    {
        var race = await raceService.CreateAsync(request, GetUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { raceId = race.Id }, race);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<RaceDto>>> GetAll([FromQuery] int limit = 50, [FromQuery] int offset = 0, CancellationToken ct = default) =>
        Ok(await raceService.GetAllAsync(limit, offset, ct));

    [HttpGet("{raceId:int}")]
    public async Task<ActionResult<RaceDto>> GetById(int raceId, CancellationToken ct) =>
        Ok(await raceService.GetByIdAsync(raceId, ct));

    [HttpPut("{raceId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceDto>> Update(int raceId, UpdateRaceRequest request, CancellationToken ct) =>
        Ok(await raceService.UpdateAsync(raceId, request, GetUserId(), ct));

    /// <summary>Historial de modificaciones de la carrera (más reciente primero). Solo administradores.</summary>
    [HttpGet("{raceId:int}/audit")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<List<AuditLogDto>>> GetAudit(
        int raceId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken ct = default) =>
        Ok(await auditService.GetHistoryAsync(AuditEntityTypes.Race, raceId, limit, before, ct));

    [HttpDelete("{raceId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<IActionResult> Delete(int raceId, CancellationToken ct)
    {
        await raceService.DeleteAsync(raceId, ct);
        return NoContent();
    }

    [HttpPost("{raceId:int}/start")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<RaceDto>> Start(int raceId, CancellationToken ct) =>
        Ok(await raceService.StartAsync(raceId, ct));

    [HttpPost("join")]
    public async Task<ActionResult<RaceDto>> Join(JoinByCodeRequest request, CancellationToken ct) =>
        Ok(await raceService.JoinByCodeAsync(request, GetUserId(), ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
