using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Controversias;
using NicaRunner.Application.Controversias.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// Controversias race-scoped. ASSUMPTION: GET Admin|Lector; PATCH solo Admin.
[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/controversias")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/controversias")]
[Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Lector)}")]
public class ControversiasController(IDisputeService disputeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TimingDisputeDto>>> GetAll(
        int raceId,
        [FromQuery] DisputeEstado? estado,
        [FromQuery] string? search,
        CancellationToken ct) =>
        Ok(await disputeService.ListAsync(raceId, estado, search, ct));

    [HttpGet("open-count")]
    public async Task<ActionResult<OpenDisputeCountDto>> GetOpenCount(int raceId, CancellationToken ct) =>
        Ok(new OpenDisputeCountDto(await disputeService.CountOpenAsync(raceId, ct)));

    [HttpPatch("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<TimingDisputeDto>> Resolve(
        int raceId,
        int id,
        ResolveDisputeRequest request,
        CancellationToken ct) =>
        Ok(await disputeService.ResolveAsync(
            raceId, id, request, GetUserId(), GetUserRole(), ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private UserRole GetUserRole() =>
        Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);
}
