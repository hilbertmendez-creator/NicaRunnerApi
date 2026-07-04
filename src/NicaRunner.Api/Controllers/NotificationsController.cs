using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Notifications;
using NicaRunner.Application.Notifications.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

// Este controller usa rutas absolutas por action (no un [Route] a nivel de clase)
// porque los endpoints viven en 3 recursos distintos (races, results,
// notifications). Cada action expone la versión legacy `/api/...` y la nueva
// `/api/v{version:apiVersion}/...` — mismo esquema que el resto de controllers.
[ApiController]
[ApiVersion("1.0")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpPost("/api/races/{raceId:int}/notify-all")]
    [HttpPost("/api/v{version:apiVersion}/races/{raceId:int}/notify-all")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<NotifyAllSummaryDto>> NotifyAll(int raceId, CancellationToken ct) =>
        Ok(await notificationService.NotifyAllAsync(raceId, ct));

    [HttpPost("/api/results/{resultId:int}/notify")]
    [HttpPost("/api/v{version:apiVersion}/results/{resultId:int}/notify")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<List<NotificationDto>>> NotifyResult(int resultId, CancellationToken ct) =>
        Ok(await notificationService.NotifyResultAsync(resultId, ct));

    [HttpGet("/api/notifications/{id:int}")]
    [HttpGet("/api/v{version:apiVersion}/notifications/{id:int}")]
    [Authorize]
    public async Task<ActionResult<NotificationDto>> GetStatus(int id, CancellationToken ct) =>
        Ok(await notificationService.GetStatusAsync(id, ct));
}
