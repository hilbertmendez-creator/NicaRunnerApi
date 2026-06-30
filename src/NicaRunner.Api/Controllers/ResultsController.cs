using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[Route("api/races/{raceId:int}/results")]
[Authorize]
public class ResultsController(IResultService resultService) : ControllerBase
{
    // Idempotency-Key (header opcional): el cliente envía un identificador
    // único por captura (UUID generado en el dispositivo) ANTES del POST y lo
    // reusa en cada retry. Reintentos con el mismo key contra la misma carrera
    // devuelven el Result ya creado en vez de duplicarlo. Pensado para la app
    // móvil del capturista en zonas con señal mala donde el response del POST
    // se pierde. Sin el header se preserva el comportamiento legacy.
    //
    // Si dos POSTs concurrentes con el mismo key entran a la vez, gana el que
    // commitea primero; el segundo cae en el catch de DbUpdateException del
    // service y devuelve el mismo resultado del ganador (ver ResultService).
    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<ResultDto>> Create(
        int raceId,
        CreateResultRequest request,
        [FromHeader(Name = "Idempotency-Key"), MaxLength(64)] string? idempotencyKey,
        CancellationToken ct)
    {
        var result = await resultService.CreateAsync(raceId, request, GetUserId(), idempotencyKey, ct);
        return CreatedAtAction(nameof(GetById), new { raceId, resultId = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<ResultDto>>> GetAll(int raceId, CancellationToken ct) =>
        Ok(await resultService.GetAllByRaceAsync(raceId, ct));

    [HttpGet("{resultId:int}")]
    public async Task<ActionResult<ResultDto>> GetById(int raceId, int resultId, CancellationToken ct) =>
        Ok(await resultService.GetByIdAsync(raceId, resultId, ct));

    [HttpPut("{resultId:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<ResultDto>> Update(int raceId, int resultId, UpdateResultRequest request, CancellationToken ct) =>
        Ok(await resultService.UpdateAsync(raceId, resultId, request, GetUserId(), ct));

    [HttpGet("{resultId:int}/audit")]
    public async Task<ActionResult<List<ResultAuditDto>>> GetAudit(int raceId, int resultId, CancellationToken ct) =>
        Ok(await resultService.GetAuditAsync(raceId, resultId, ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
