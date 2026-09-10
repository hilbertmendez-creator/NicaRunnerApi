using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Infrastructure.Security;

namespace NicaRunner.Api.Auth;

// backoffice-user-status-toggle PR3 (design.md D1/D4). Cuelga de OnTokenValidated, no de
// una policy de autorización: una policy que falla produce 403, y ni el web
// (client.ts:91-128) ni el móvil (TokenAuthenticator.kt) reintentan /auth/refresh ante un
// 403 -- solo ante el 401 que context.Fail() produce acá. El try/catch vive en este
// método (no en IAccountStatusCache) para que la política de fail-open quede en un solo
// sitio: cualquier falla de infraestructura permite + LogWarning (un bug acá bloquearía a
// todos los usuarios a la vez). No static: ILogger<AccountStatusJwtEvents> no acepta un
// tipo estático como argumento genérico (CS0718).
public class AccountStatusJwtEvents
{
    public static async Task OnTokenValidated(TokenValidatedContext context)
    {
        var services = context.HttpContext.RequestServices;
        var options = services.GetRequiredService<IOptions<AccountStatusOptions>>().Value;
        if (!options.EnforcePerRequest)
            return;

        var logger = services.GetRequiredService<ILogger<AccountStatusJwtEvents>>();

        var claim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId))
        {
            // Fail-open: la firma ya se validó, un token sin subject es bug propio (D4).
            logger.LogWarning(
                "AccountStatus: claim NameIdentifier ausente o no numérico ({Claim}); se permite la solicitud (fail-open).",
                claim);
            return;
        }

        var cache = services.GetRequiredService<IAccountStatusCache>();
        bool isActive;
        try
        {
            isActive = await cache.IsActiveAsync(userId, context.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            // Cubre cache/BD caídos y usuario borrado (NotFoundException) -- D4: permitir.
            logger.LogWarning(ex,
                "AccountStatus: no se pudo verificar el estado de la cuenta {UserId}; se permite la solicitud (fail-open).",
                userId);
            return;
        }

        if (!isActive)
        {
            logger.LogInformation("AccountStatus: usuario {UserId} está desactivado; se rechaza la solicitud.", userId);
            context.Fail("La cuenta está desactivada.");
        }
    }
}
