using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Auditing.Dtos;
using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Races;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/races")]
[Route("api/v{version:apiVersion}/races")]
[Authorize]
public class RacesController(IRaceService raceService, IAuditService auditService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceDto>> Create(CreateRaceRequest request, CancellationToken ct)
    {
        var race = await raceService.CreateAsync(request, GetUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { raceId = race.Id }, race);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<RaceDto>>> GetAll([FromQuery] int limit = 50, [FromQuery] int offset = 0, CancellationToken ct = default) =>
        Ok(await raceService.GetAllAsync(limit, offset, ct));

    [HttpGet("{raceId:int}")]
    public async Task<ActionResult<RaceDto>> GetById(int raceId, CancellationToken ct) =>
        Ok(await raceService.GetByIdAsync(raceId, ct));

    [HttpPut("{raceId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceDto>> Update(int raceId, UpdateRaceRequest request, CancellationToken ct) =>
        Ok(await raceService.UpdateAsync(raceId, request, GetUserId(), ct));

    /// <summary>Historial de modificaciones de la carrera (más reciente primero). Solo administradores.</summary>
    [HttpGet("{raceId:int}/audit")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<List<AuditLogDto>>> GetAudit(
        int raceId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken ct = default) =>
        Ok(await auditService.GetHistoryAsync(AuditEntityTypes.Race, raceId, limit, before, ct));

    [HttpDelete("{raceId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<IActionResult> Delete(int raceId, CancellationToken ct)
    {
        await raceService.DeleteAsync(raceId, ct);
        return NoContent();
    }

    [HttpPost("{raceId:int}/start")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<RaceDto>> Start(int raceId, CancellationToken ct) =>
        Ok(await raceService.StartAsync(raceId, ct));

    [HttpPost("join")]
    public async Task<ActionResult<RaceDto>> Join(JoinByCodeRequest request, CancellationToken ct) =>
        Ok(await raceService.JoinByCodeAsync(request, GetUserId(), ct));

    /// <summary>Cierra una carrera cascadeando sobre sus categorías EnCurso.</summary>
    /// <remarks>
    /// Sin body. `Race.Estado` es derivado (nunca se setea directamente): esto transiciona
    /// cada `RaceCategory` EnCurso a Terminada y el estado de la carrera se recalcula solo,
    /// en la misma transacción — por eso sobrevive a cualquier transición posterior.
    /// Restringido a Administrador y Capturista, igual que `POST /start`.
    ///
    /// Precondiciones (server-side, un juez puede tener datos desactualizados):
    /// - Ningún `Result` en Controversia sin resolver.
    /// - Ninguna captura sin dorsal asignado (excepto las Anuladas — nunca van a recibir uno).
    /// - Ninguna `RaceCategory` todavía Planeada (nunca se cierra sola como efecto secundario).
    ///
    /// Idempotente: cerrar una carrera ya Terminada devuelve 200 sin mutar nada ni auditar.
    ///
    /// Cuando quien cierra es Capturista, se notifica por email a todos los Administradores
    /// (una falla de envío nunca hace fallar el cierre — la bitácora es el registro durable).
    ///
    /// Ejemplo de respuesta 200:
    /// ```json
    /// { "id": 42, "nombre": "5K Managua", "estado": "Terminada", "joinCode": "AB12CD", ... }
    /// ```
    ///
    /// Ejemplo de respuesta 409:
    /// ```json
    /// { "status": 409, "title": "Conflict", "detail": "No se puede cerrar: hay 2 llegada(s) en controversia sin resolver." }
    /// ```
    /// </remarks>
    /// <response code="200">La carrera quedó (o ya estaba) Terminada.</response>
    /// <response code="400">El token no trae un id de usuario válido.</response>
    /// <response code="401">Falta autenticación.</response>
    /// <response code="403">El rol autenticado no es Administrador ni Capturista.</response>
    /// <response code="409">Hay disputas abiertas, capturas sin dorsal, o categorías todavía Planeada.</response>
    [HttpPost("{raceId:int}/close")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    [ProducesResponseType(typeof(RaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RaceDto>> Close(int raceId, CancellationToken ct) =>
        Ok(await raceService.CloseAsync(raceId, GetUserId(), ct));

    /// <summary>Reabre una carrera Terminada cascadeando sobre sus categorías Terminada.</summary>
    /// <remarks>
    /// Sin body. Mismo principio que Close: nunca escribe `Race.Estado` directamente, cascadea
    /// sobre `RaceCategory` (Terminada -> EnCurso) y el estado deriva solo — por eso, a
    /// diferencia del reopen previo vía `PUT /races/{id}`, esto SOBREVIVE a una transición
    /// posterior de categoría en vez de revertirse silenciosamente.
    /// Restringido a Administrador únicamente.
    ///
    /// Idempotente: reabrir una carrera que ya no está Terminada devuelve 200 sin mutar nada
    /// ni auditar.
    ///
    /// Ejemplo de respuesta 200:
    /// ```json
    /// { "id": 42, "nombre": "5K Managua", "estado": "EnCurso", "joinCode": "AB12CD", ... }
    /// ```
    /// </remarks>
    /// <response code="200">La carrera quedó (o ya estaba) EnCurso.</response>
    /// <response code="401">Falta autenticación.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">No existe una carrera con ese id.</response>
    [HttpPost("{raceId:int}/reopen")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    [ProducesResponseType(typeof(RaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceDto>> Reopen(int raceId, CancellationToken ct) =>
        Ok(await raceService.ReopenAsync(raceId, GetUserId(), ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
