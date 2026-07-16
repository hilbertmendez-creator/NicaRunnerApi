using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Auditing.Dtos;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// Catálogo global de categorías (backoffice), independiente de cualquier carrera.
[ApiController]
[ApiVersion("1.0")]
[Route("api/categories")]
[Route("api/v{version:apiVersion}/categories")]
[Authorize]
public class CategoriesController(ICategoryService categoryService, IAuditService auditService) : ControllerBase
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
        Ok(await categoryService.UpdateAsync(categoryId, request, GetUserId(), ct));

    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<IActionResult> Delete(int categoryId, CancellationToken ct)
    {
        await categoryService.DeleteAsync(categoryId, ct);
        return NoContent();
    }

    /// <summary>Historial de modificaciones de la categoría (más reciente primero). Solo administradores.</summary>
    [HttpGet("{categoryId:int}/audit")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<List<AuditLogDto>>> GetAudit(
        int categoryId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken ct = default) =>
        Ok(await auditService.GetHistoryAsync(AuditEntityTypes.Category, categoryId, limit, before, ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
