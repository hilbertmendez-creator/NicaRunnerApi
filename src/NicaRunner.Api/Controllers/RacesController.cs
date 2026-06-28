using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Races;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[Route("api/races")]
[Authorize]
public class RacesController(IRaceService raceService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceDto>> Create(CreateRaceRequest request, CancellationToken ct)
    {
        var race = await raceService.CreateAsync(request, GetUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { raceId = race.Id }, race);
    }

    [HttpGet]
    public async Task<ActionResult<List<RaceDto>>> GetAll(CancellationToken ct) =>
        Ok(await raceService.GetAllAsync(ct));

    [HttpGet("{raceId:int}")]
    public async Task<ActionResult<RaceDto>> GetById(int raceId, CancellationToken ct) =>
        Ok(await raceService.GetByIdAsync(raceId, ct));

    [HttpPut("{raceId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceDto>> Update(int raceId, UpdateRaceRequest request, CancellationToken ct) =>
        Ok(await raceService.UpdateAsync(raceId, request, ct));

    [HttpDelete("{raceId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<IActionResult> Delete(int raceId, CancellationToken ct)
    {
        await raceService.DeleteAsync(raceId, ct);
        return NoContent();
    }

    [HttpPost("{raceId:int}/start")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceDto>> Start(int raceId, CancellationToken ct) =>
        Ok(await raceService.StartAsync(raceId, ct));

    [HttpPost("join")]
    public async Task<ActionResult<RaceDto>> Join(JoinByCodeRequest request, CancellationToken ct) =>
        Ok(await raceService.JoinByCodeAsync(request, GetUserId(), ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
