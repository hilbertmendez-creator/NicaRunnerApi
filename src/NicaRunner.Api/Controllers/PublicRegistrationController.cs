using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NicaRunner.Application.Registrations;
using NicaRunner.Application.Registrations.Dtos;

namespace NicaRunner.Api.Controllers;

// public-runner-registration-manual-payment: mismo patrón que PublicResultsController
// (AllowAnonymous + rate limit por IP). "public-registration" se registra en Program.cs
// ya en esta fase (no se difiere a Phase 4) porque un endpoint anotado con una policy de
// rate-limit no registrada lanza en tiempo de request, no de arranque — diferirlo
// rompería cualquier prueba que ejercite este controller antes de Phase 4.
[ApiController]
[ApiVersion("1.0")]
[Route("api/public/registrations")]
[Route("api/v{version:apiVersion}/public/registrations")]
[AllowAnonymous]
[EnableRateLimiting("public-registration")]
public class PublicRegistrationController(IRegistrationService registrationService) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<ActionResult<RegistrationLinkInfoDto>> GetLinkInfo(string token, CancellationToken ct) =>
        Ok(await registrationService.GetLinkInfoAsync(token, ct));

    [HttpPost("{token}")]
    public async Task<ActionResult<RegistrationDto>> Submit(string token, SubmitRegistrationRequest request, CancellationToken ct) =>
        Ok(await registrationService.SubmitAsync(token, request, ct));

    [HttpPut("{token}/{registrationId:int}/receipt")]
    public async Task<ActionResult<RegistrationDto>> UploadReceipt(string token, int registrationId, UploadReceiptRequest request, CancellationToken ct) =>
        Ok(await registrationService.UploadReceiptAsync(token, registrationId, request, ct));
}
