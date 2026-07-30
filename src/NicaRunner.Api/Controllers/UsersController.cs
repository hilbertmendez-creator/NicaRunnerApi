using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Auditing.Dtos;
using NicaRunner.Application.Users;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/users")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class UsersController(
    IUserManagementService userManagementService,
    IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken ct) =>
        Ok(await userManagementService.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await userManagementService.CreateAsync(request, ct);
        return Ok(user);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await userManagementService.UpdateAsync(GetUserId(), id, request, ct));

    /// <summary>login-lockout: "Admin Unlock" — limpia el lock de una cuenta bloqueada. Solo administradores.</summary>
    [HttpPost("{id:int}/unlock")]
    public async Task<ActionResult<UserDto>> Unlock(int id, CancellationToken ct) =>
        Ok(await userManagementService.UnlockAsync(GetUserId(), id, ct));

    /// <summary>Historial de modificaciones del usuario (más reciente primero). Solo administradores.</summary>
    [HttpGet("{id:int}/audit")]
    public async Task<ActionResult<List<AuditLogDto>>> GetAudit(
        int id, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken ct = default) =>
        Ok(await auditService.GetHistoryAsync(AuditEntityTypes.User, id, limit, before, ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
