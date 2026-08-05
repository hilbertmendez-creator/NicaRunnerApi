using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Registrations;
using NicaRunner.Application.Registrations.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// registration-review spec.md "Registration Link Administration" (tasks.md 2.12):
// create/list/revoke the public RegistrationLink for a race. Mirrors PublicTokensController
// (design.md D4), plus Revoke — which PublicResultToken's admin surface doesn't have, but
// this spec explicitly requires it.
[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/registration-link")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/registration-link")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class RegistrationLinksController(IRegistrationService registrationService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RegistrationLinkDto>> Create(int raceId, CreateRegistrationLinkRequest request, CancellationToken ct)
    {
        var link = await registrationService.CreateLinkAsync(raceId, request, GetUserId(), ct);
        return Ok(link);
    }

    [HttpGet]
    public async Task<ActionResult<List<RegistrationLinkDto>>> GetAll(int raceId, CancellationToken ct) =>
        Ok(await registrationService.GetAllLinksAsync(raceId, ct));

    [HttpPost("{linkId:int}/revoke")]
    public async Task<IActionResult> Revoke(int raceId, int linkId, CancellationToken ct)
    {
        await registrationService.RevokeLinkAsync(raceId, linkId, ct);
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
