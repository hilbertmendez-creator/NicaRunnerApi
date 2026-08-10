using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.AdminNotifications.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/admin-notifications")]
[Route("api/v{version:apiVersion}/admin-notifications")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class AdminNotificationsController(IAdminNotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminNotificationsPageDto>> GetRecent([FromQuery] int limit = 20, CancellationToken ct = default) =>
        Ok(await service.GetRecentAsync(limit, ct));

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await service.MarkReadAsync(id, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await service.MarkAllReadAsync(ct);
        return NoContent();
    }
}
