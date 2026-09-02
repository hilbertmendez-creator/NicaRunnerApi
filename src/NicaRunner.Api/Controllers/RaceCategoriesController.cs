using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/races/{raceId:int}/categories")]
[Route("api/v{version:apiVersion}/races/{raceId:int}/categories")]
[Authorize]
public class RaceCategoriesController(IRaceCategoryService categoryService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceCategoryDto>> Assign(int raceId, AssignCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.AssignAsync(raceId, request, ct);
        return CreatedAtAction(nameof(GetAll), new { raceId }, category);
    }

    [HttpGet]
    public async Task<ActionResult<List<RaceCategoryDto>>> GetAll(int raceId, CancellationToken ct) =>
        Ok(await categoryService.GetAllByRaceAsync(raceId, ct));

    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<IActionResult> Unassign(int raceId, int categoryId, CancellationToken ct)
    {
        await categoryService.UnassignAsync(raceId, categoryId, ct);
        return NoContent();
    }

    // El juez de partida es Capturista: no puede ser Admin-only como Assign/Unassign.
    // Idempotency-Key opcional, mismo patrón que ResultsController.Create — necesario
    // para que un reintento offline (señal que va y viene) no falle sobre su propio
    // éxito anterior.
    [HttpPost("start")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<List<RaceCategoryDto>>> Start(
        int raceId,
        CategoryTransitionRequest request,
        [FromHeader(Name = "Idempotency-Key"), MaxLength(64)] string? idempotencyKey,
        CancellationToken ct)
    {
        var effective = request with { IdempotencyKey = idempotencyKey };
        return Ok(await categoryService.StartAsync(raceId, effective, GetUserId(), ct));
    }

    // PR 2b, motivo CategoriaSinSalida: corrige el arranque de una categoría Planeada
    // que nunca se disparó. Admin-only — a diferencia de `start` (Admin + Capturista,
    // el "Dar salida" en tiempo real del juez), esto es una corrección administrativa
    // retroactiva, mismo criterio que `reopen`.
    [HttpPost("{categoryId:int}/correct-start")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceCategoryDto>> CorrectStart(
        int raceId, int categoryId, CorrectCategoryStartRequest request, CancellationToken ct) =>
        Ok(await categoryService.CorrectStartAsync(raceId, categoryId, request, GetUserId(), ct));

    /// <summary>Salida en falso: devuelve categorías EnCurso a Planeada y anula sus llegadas.</summary>
    /// <remarks>
    /// Admin-only, y no por simetría con `reopen` sino porque es la única transición de
    /// categoría que DESTRUYE datos: borra el cero y anula toda captura medida contra él.
    /// Por eso también exige `razon`, que `start` no pide.
    ///
    /// No confundir con las otras dos correcciones de arranque:
    /// `correct-start` le pone hora a una categoría que nunca salió; `reopen` deshace un
    /// cierre equivocado sin tocar el cero. Esto deshace la SALIDA misma.
    /// </remarks>
    /// <response code="200">Las categorías volvieron a Planeada; el body dice cuántas llegadas se anularon.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">Alguna categoría no está asignada a esta carrera.</response>
    /// <response code="409">Alguna categoría no está EnCurso.</response>
    [HttpPost("reset-start")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    [ProducesResponseType(typeof(ResetStartResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResetStartResultDto>> ResetStart(
        int raceId, ResetCategoryStartRequest request, CancellationToken ct) =>
        Ok(await categoryService.ResetStartAsync(raceId, request, GetUserId(), ct));

    [HttpPost("close")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<List<RaceCategoryDto>>> Close(
        int raceId, CategoryTransitionRequest request, CancellationToken ct) =>
        Ok(await categoryService.CloseAsync(raceId, request, GetUserId(), ct));

    // Reabrir corrige un error: mismo criterio que la reapertura de carrera, solo Admin.
    [HttpPost("reopen")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<List<RaceCategoryDto>>> Reopen(
        int raceId, CategoryTransitionRequest request, CancellationToken ct) =>
        Ok(await categoryService.ReopenAsync(raceId, request, GetUserId(), ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
