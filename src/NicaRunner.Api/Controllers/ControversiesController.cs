using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Controversies;
using NicaRunner.Application.Controversies.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/controversies")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/controversies")]
[Authorize]
public class ControversiesController(IControversyService controversyService) : ControllerBase
{
    // Listado por carrera. Solo lectura; lectura autenticada (backoffice).
    [HttpGet]
    public async Task<ActionResult<List<ControversyDto>>> GetAll(int raceId, CancellationToken ct) =>
        Ok(await controversyService.GetAllByRaceAsync(raceId, ct));

    // Resumen de conteos por estado — alimenta la nav-badge del sidebar.
    [HttpGet("summary")]
    public async Task<ActionResult<ControversySummaryDto>> GetSummary(int raceId, CancellationToken ct) =>
        Ok(await controversyService.GetSummaryAsync(raceId, ct));

    // Resolver una disputa cambia su estado (Abierta|Resuelta). Solo admin,
    // siguiendo el precedente de ResultsController.Update / ResultsController.GetAudit.
    [HttpPost("{id:int}/resolve")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<ControversyDto>> Resolve(
        int raceId,
        int id,
        ResolveControversyRequest request,
        CancellationToken ct) =>
        Ok(await controversyService.ResolveAsync(raceId, id, request, ct));
}