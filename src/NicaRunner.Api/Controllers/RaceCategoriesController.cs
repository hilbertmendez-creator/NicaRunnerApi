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
    [HttpPost("start")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<List<RaceCategoryDto>>> Start(
        int raceId, CategoryTransitionRequest request, CancellationToken ct) =>
        Ok(await categoryService.StartAsync(raceId, request, GetUserId(), ct));

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
