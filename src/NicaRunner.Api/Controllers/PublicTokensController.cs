using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.PublicResults;
using NicaRunner.Application.PublicResults.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/public-token")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/public-token")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class PublicTokensController(IPublicResultService publicResultService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PublicTokenDto>> Create(int raceId, CreatePublicTokenRequest request, CancellationToken ct)
    {
        var token = await publicResultService.CreateTokenAsync(raceId, request, GetUserId(), ct);
        return Ok(token);
    }

    [HttpGet]
    public async Task<ActionResult<List<PublicTokenDto>>> GetAll(int raceId, CancellationToken ct) =>
        Ok(await publicResultService.GetAllByRaceAsync(raceId, ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
