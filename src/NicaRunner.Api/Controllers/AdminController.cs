using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Admin;

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
    IConfiguration configuration,
    ILogger<AdminController> logger) : ControllerBase
{
    private const string AdminSecretHeader = "X-Admin-Secret";
    private const string AdminSecretConfigKey = "Admin:CleanupSecret";

    [HttpPost("refresh-tokens/cleanup")]
    public async Task<ActionResult<CleanupResult>> CleanupRefreshTokens(CancellationToken ct)
    {
        var expected = configuration[AdminSecretConfigKey];
        if (string.IsNullOrWhiteSpace(expected))
        {
            // Sin secret configurado, el endpoint queda cerrado por default —
            // preferible al comportamiento inverso (endpoint abierto en fresh
            // deploys donde el operador todavía no seteó la variable).
            logger.LogWarning("Admin cleanup endpoint invocado pero {Key} no está configurado.", AdminSecretConfigKey);
            return Unauthorized();
        }

        if (!Request.Headers.TryGetValue(AdminSecretHeader, out var provided) || provided.ToString() != expected)
        {
            return Unauthorized();
        }

        var result = await refreshTokenCleanup.RunAsync(ct);
        logger.LogInformation("Admin cleanup borró {Deleted} refresh tokens expirados/revocados.", result.Deleted);
        return Ok(result);
    }
}
