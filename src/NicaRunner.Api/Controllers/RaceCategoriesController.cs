using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[Route("api/races/{raceId:int}/categories")]
[Authorize]
public class RaceCategoriesController(IRaceCategoryService categoryService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceCategoryDto>> Create(int raceId, CreateRaceCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(raceId, request, ct);
        return CreatedAtAction(nameof(GetAll), new { raceId }, category);
    }

    [HttpGet]
    public async Task<ActionResult<List<RaceCategoryDto>>> GetAll(int raceId, CancellationToken ct) =>
        Ok(await categoryService.GetAllByRaceAsync(raceId, ct));

    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RaceCategoryDto>> Update(int raceId, int categoryId, UpdateRaceCategoryRequest request, CancellationToken ct) =>
        Ok(await categoryService.UpdateAsync(raceId, categoryId, request, ct));

    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<IActionResult> Delete(int raceId, int categoryId, CancellationToken ct)
    {
        await categoryService.DeleteAsync(raceId, categoryId, ct);
        return NoContent();
    }
}
