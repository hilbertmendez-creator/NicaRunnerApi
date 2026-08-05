using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.ReservedDorsals;
using NicaRunner.Application.ReservedDorsals.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// design.md D9/D10 File Changes: DELETE es obligatorio, no opcional — la reserva bloquea
// también el camino manual de RunnerService.CreateAsync/UpdateAsync (D8), así que sin un
// endpoint de liberación un dorsal reservado quedaría inutilizable para siempre.
[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/reserved-dorsals")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/reserved-dorsals")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class ReservedDorsalsController(IReservedDorsalService reservedDorsalService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ReservedDorsalDto>>> GetAll(int raceId, CancellationToken ct) =>
        Ok(await reservedDorsalService.GetAllByRaceAsync(raceId, ct));

    [HttpPost]
    public async Task<ActionResult<ReservedDorsalDto>> Create(int raceId, CreateReservedDorsalRequest request, CancellationToken ct)
    {
        var reservedDorsal = await reservedDorsalService.CreateAsync(raceId, request, GetUserId(), ct);
        return Ok(reservedDorsal);
    }

    [HttpDelete("{reservedDorsalId:int}")]
    public async Task<IActionResult> Delete(int raceId, int reservedDorsalId, CancellationToken ct)
    {
        await reservedDorsalService.DeleteAsync(raceId, reservedDorsalId, ct);
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
