using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/results")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/results")]
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
    public async Task<ActionResult<PaginatedList<ResultDto>>> GetAll(int raceId, [FromQuery] int limit = 50, [FromQuery] int offset = 0, CancellationToken ct = default) =>
        Ok(await resultService.GetAllByRaceAsync(raceId, limit, offset, ct));

    [HttpGet("{resultId:int}")]
    public async Task<ActionResult<ResultDto>> GetById(int raceId, int resultId, CancellationToken ct) =>
        Ok(await resultService.GetByIdAsync(raceId, resultId, ct));

    [HttpPut("{resultId:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<ResultDto>> Update(int raceId, int resultId, UpdateResultRequest request, CancellationToken ct) =>
        Ok(await resultService.UpdateAsync(raceId, resultId, request, GetUserId(), ct));

    // Historial de auditoría: spec sección 6 lo agrupa bajo "Edición de
    // Resultados (Admin)", no bajo "Dashboard (Admin & Lector)" — a
    // diferencia de dashboard/standings, acá Lector NO tiene acceso.
    [HttpGet("{resultId:int}/audit")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<List<ResultAuditDto>>> GetAudit(int raceId, int resultId, CancellationToken ct) =>
        Ok(await resultService.GetAuditAsync(raceId, resultId, ct));

    // F4 del spec: el autor deshace lo suyo mientras la carrera sigue EnCurso; un
    // Admin deshace cualquier captura, siempre. isAdmin se resuelve del rol del
    // token, no de un parámetro que el cliente pudiera falsear.
    [HttpPost("{resultId:int}/void")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<ResultDto>> Void(int raceId, int resultId, VoidResultRequest request, CancellationToken ct)
    {
        var isAdmin = User.IsInRole(nameof(UserRole.Administrador));
        return Ok(await resultService.VoidAsync(raceId, resultId, request, GetUserId(), isAdmin, ct));
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
