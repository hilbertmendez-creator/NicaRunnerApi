using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Admin;
using NicaRunner.Application.Notifications;
using NicaRunner.Application.Notifications.Dtos;

namespace NicaRunner.Api.Controllers;

/// <summary>
/// Endpoints administrativos disparados por sistemas externos (cron de
/// GitHub Actions, on-call scripts). No usan JWT — son máquina-a-máquina.
/// Protegidos con un header X-Admin-Secret que se compara contra la config
/// Admin:CleanupSecret. En producción esa clave la setea el operador desde
/// el dashboard de Render (ver docs/render-setup.md); si falta o no coincide,
/// el endpoint responde 401 sin dar detalles.
/// </summary>
[ApiController]
[Route("api/admin")]
[AllowAnonymous]
public class AdminController(
    IRefreshTokenCleanupService refreshTokenCleanup,
    IPublicTokenCleanupService publicTokenCleanup,
    INotificationService notificationService,
    IConfiguration configuration,
    ILogger<AdminController> logger) : ControllerBase
{
    private const string AdminSecretHeader = "X-Admin-Secret";
    private const string AdminSecretConfigKey = "Admin:CleanupSecret";

    [HttpPost("refresh-tokens/cleanup")]
    public async Task<ActionResult<CleanupResult>> CleanupRefreshTokens(CancellationToken ct)
    {
        if (!IsAuthorized("refresh-tokens/cleanup"))
            return Unauthorized();

        var result = await refreshTokenCleanup.RunAsync(ct);
        logger.LogInformation("Admin cleanup borró {Deleted} refresh tokens expirados/revocados.", result.Deleted);
        return Ok(result);
    }

    [HttpPost("public-tokens/cleanup")]
    public async Task<ActionResult<CleanupResult>> CleanupPublicTokens(CancellationToken ct)
    {
        if (!IsAuthorized("public-tokens/cleanup"))
            return Unauthorized();

        var result = await publicTokenCleanup.RunAsync(ct);
        logger.LogInformation("Admin cleanup borró {Deleted} tokens públicos expirados.", result.Deleted);
        return Ok(result);
    }

    [HttpPost("notifications/process-pending")]
    public async Task<ActionResult<NotificationProcessSummaryDto>> ProcessPendingNotifications(CancellationToken ct)
    {
        if (!IsAuthorized("notifications/process-pending"))
            return Unauthorized();

        // El barrido periódico lo hace PendingNotificationsWorker in-process;
        // este endpoint queda para disparos manuales (on-call, debug).
        var result = await notificationService.ProcessPendingAsync(ct);
        logger.LogInformation(
            "Admin notifications sweep: {Procesadas} procesadas, {Enviadas} enviadas, {Fallidas} fallidas.",
            result.Procesadas, result.Enviadas, result.Fallidas);
        return Ok(result);
    }

    private bool IsAuthorized(string endpoint)
    {
        var expected = configuration[AdminSecretConfigKey];
        if (string.IsNullOrWhiteSpace(expected))
        {
            // Sin secret configurado, el endpoint queda cerrado por default —
            // preferible al comportamiento inverso (endpoint abierto en fresh
            // deploys donde el operador todavía no seteó la variable).
            logger.LogWarning("Admin endpoint {Endpoint} invocado pero {Key} no está configurado.", endpoint, AdminSecretConfigKey);
            return false;
        }

        return Request.Headers.TryGetValue(AdminSecretHeader, out var provided) && provided.ToString() == expected;
    }
}
