using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Registrations;
using NicaRunner.Application.Registrations.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// registration-review spec.md: list/confirm/reject + bulk-Excel confirm
// (GET confirm-template / POST confirm-bulk, tasks.md 3.4), admin-only.
[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/registrations")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/registrations")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class RegistrationsController(IRegistrationService registrationService) : ControllerBase
{
    private const long MaxExcelFileSizeBytes = 5 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<List<RegistrationDto>>> GetAll(int raceId, [FromQuery] RegistrationStatus? estado, CancellationToken ct) =>
        Ok(await registrationService.GetAllForReviewAsync(raceId, estado, ct));

    [HttpPost("{registrationId:int}/confirm")]
    public async Task<ActionResult<RegistrationDto>> Confirm(int raceId, int registrationId, ConfirmRegistrationRequest request, CancellationToken ct) =>
        Ok(await registrationService.ConfirmAsync(raceId, registrationId, request, GetUserId(), ct));

    [HttpPost("{registrationId:int}/reject")]
    public async Task<ActionResult<RegistrationDto>> Reject(int raceId, int registrationId, RejectRegistrationRequest request, CancellationToken ct) =>
        Ok(await registrationService.RejectAsync(raceId, registrationId, request, GetUserId(), ct));

    // registration-review spec.md "Bulk Confirm via Excel Template" — scenario "Template
    // download scoped to pending registrations".
    [HttpGet("confirm-template")]
    public async Task<IActionResult> DownloadConfirmTemplate(int raceId, CancellationToken ct)
    {
        var content = await registrationService.GenerateBulkConfirmTemplateAsync(raceId, ct);
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"plantilla-confirmacion-inscripciones-carrera-{raceId}.xlsx");
    }

    // registration-review spec.md "Bulk Confirm via Excel Template" — partial-success and
    // capacity-exhausted-mid-batch scenarios. Upload guards (.xlsx, 5 MB) copied from
    // RunnersController.ImportExcel.
    [HttpPost("confirm-bulk")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BulkConfirmResultDto>> ConfirmBulk(int raceId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Debe adjuntar un archivo Excel (.xlsx) con al menos una fila de datos.");

        if (file.Length > MaxExcelFileSizeBytes)
            return BadRequest("El archivo excede el tamaño máximo permitido (5 MB).");

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("El archivo debe tener extensión .xlsx.");

        await using var stream = file.OpenReadStream();
        var result = await registrationService.ConfirmBulkAsync(raceId, stream, GetUserId(), ct);
        return Ok(result);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
