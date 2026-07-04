using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// Catálogo global de categorías (backoffice), independiente de cualquier carrera.
[ApiController]
[ApiVersion("1.0")]
[Route("api/categories")]
[Route("api/v{version:apiVersion}/categories")]
[Authorize]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { categoryId = category.Id }, category);
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken ct) =>
        Ok(await categoryService.GetAllAsync(ct));

    [HttpGet("{categoryId:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int categoryId, CancellationToken ct) =>
        Ok(await categoryService.GetByIdAsync(categoryId, ct));

    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<CategoryDto>> Update(int categoryId, UpdateCategoryRequest request, CancellationToken ct) =>
        Ok(await categoryService.UpdateAsync(categoryId, request, ct));

    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<IActionResult> Delete(int categoryId, CancellationToken ct)
    {
        await categoryService.DeleteAsync(categoryId, ct);
        return NoContent();
    }
}
