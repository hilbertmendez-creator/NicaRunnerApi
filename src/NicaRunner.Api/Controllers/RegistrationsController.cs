using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Registrations;
using NicaRunner.Application.Registrations.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// registration-review spec.md: list/confirm/reject, admin-only. Bulk-Excel confirm
// (GET confirm-template / POST confirm-bulk) llega en Phase 3 (tasks.md 3.4) — no se
// implementa acá.
[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/registrations")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/registrations")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class RegistrationsController(IRegistrationService registrationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RegistrationDto>>> GetAll(int raceId, [FromQuery] RegistrationStatus? estado, CancellationToken ct) =>
        Ok(await registrationService.GetAllForReviewAsync(raceId, estado, ct));

    [HttpPost("{registrationId:int}/confirm")]
    public async Task<ActionResult<RegistrationDto>> Confirm(int raceId, int registrationId, ConfirmRegistrationRequest request, CancellationToken ct) =>
        Ok(await registrationService.ConfirmAsync(raceId, registrationId, request, GetUserId(), ct));

    [HttpPost("{registrationId:int}/reject")]
    public async Task<ActionResult<RegistrationDto>> Reject(int raceId, int registrationId, RejectRegistrationRequest request, CancellationToken ct) =>
        Ok(await registrationService.RejectAsync(raceId, registrationId, request, GetUserId(), ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
