using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Admin;
using NicaRunner.Application.Notifications;
using NicaRunner.Application.Notifications.Dtos;
using NicaRunner.Application.Races;
using NicaRunner.Application.Races.Dtos;

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
    IStaleRaceSweepService staleRaceSweep,
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

        var result = await notificationService.ProcessPendingAsync(ct);
        logger.LogInformation(
            "Admin notifications sweep: {Procesadas} procesadas, {Enviadas} enviadas, {Fallidas} fallidas.",
            result.Procesadas, result.Enviadas, result.Fallidas);
        return Ok(result);
    }

    /// <summary>
    /// Guardia anti-zombie: detecta carreras EnCurso sin actividad de captura hace 12
    /// horas o más y notifica por email a todos los Administradores.
    /// </summary>
    /// <remarks>
    /// "Actividad" es el último `Result` capturado en la carrera (cualquier Estado,
    /// incluido Anulado — un Deshacer también es un juez presente); si la carrera
    /// todavía no tiene ningún `Result`, se usa `Race.RaceStartUtc` en su lugar.
    ///
    /// NUNCA cierra ni muta ninguna carrera — solo alerta. Un fallo del proveedor de
    /// email no hace fallar el barrido (mismo criterio que POST /races/{id}/close): la
    /// lista devuelta en el body es el registro que importa para el cron/diagnóstico.
    ///
    /// Ejemplo de respuesta 200:
    /// ```json
    /// { "staleRaces": [{ "raceId": 42, "nombre": "5K Managua", "lastActivityUtc": "2026-08-09T10:00:00Z" }] }
    /// ```
    /// </remarks>
    /// <response code="200">Barrido corrido. `staleRaces` puede venir vacía.</response>
    /// <response code="401">Falta o no coincide X-Admin-Secret.</response>
    [HttpPost("races/stale-sweep")]
    [ProducesResponseType(typeof(StaleRaceSweepResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StaleRaceSweepResult>> StaleRaceSweep(CancellationToken ct)
    {
        if (!IsAuthorized("races/stale-sweep"))
            return Unauthorized();

        var result = await staleRaceSweep.RunAsync(ct);
        logger.LogInformation(
            "Admin stale-race sweep: {Count} carrera(s) sin actividad reciente.", result.StaleRaces.Count);
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

        if (!Request.Headers.TryGetValue(AdminSecretHeader, out var provided))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
