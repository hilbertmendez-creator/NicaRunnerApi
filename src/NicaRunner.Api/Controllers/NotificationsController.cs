using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Notifications;
using NicaRunner.Application.Notifications.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpPost("/api/races/{raceId:int}/notify-all")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<NotifyAllSummaryDto>> NotifyAll(int raceId, CancellationToken ct) =>
        Ok(await notificationService.NotifyAllAsync(raceId, ct));

    [HttpPost("/api/results/{resultId:int}/notify")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<List<NotificationDto>>> NotifyResult(int resultId, CancellationToken ct) =>
        Ok(await notificationService.NotifyResultAsync(resultId, ct));

    [HttpGet("/api/notifications/{id:int}")]
    [Authorize]
    public async Task<ActionResult<NotificationDto>> GetStatus(int id, CancellationToken ct) =>
        Ok(await notificationService.GetStatusAsync(id, ct));
}
