using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Infrastructure.Security;

namespace NicaRunner.Api.Auth;

// Hasta acá el cambio obligatorio de contraseña lo exigía sólo el panel
// (RequirePasswordChanged.tsx): con la contraseña temporal y la API a mano se usaba
// la cuenta sin cambiarla nunca. Esto lo exige también el servidor, con dos límites
// a propósito:
//
// - Sólo para la sesión del panel (el JWT llegó en la cookie nr_at). La app Android
//   manda Authorization y no tiene pantalla de cambio: exigírselo dejaría a los
//   capturistas sin poder capturar el día de la carrera.
// - Sólo para cuentas con contraseña (IsPasswordChangePendingAsync).
//
// 403 y no 401: ante un 401 el cliente web pide /auth/refresh y reintenta, y acá eso
// sería un bucle. Igual que AccountStatusJwtEvents, una falla al consultar deja pasar:
// un bug acá trabaría a todo el panel a la vez.
public class PasswordChangeRequiredMiddleware(RequestDelegate next, ILogger<PasswordChangeRequiredMiddleware> logger)
{
    // Todo AuthController queda afuera: la pantalla de cambio necesita /me, el cambio y
    // la salida, y login o reset-password tienen que andar aunque el navegador todavía
    // mande la cookie de una sesión anterior (la misma razón de csrfExemptPaths).
    private static readonly Regex AuthPath =
        new(@"^/api/(v[^/]+/)?auth(/|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task InvokeAsync(HttpContext context, IAccountStatusCache cache, IOptions<AccountStatusOptions> options)
    {
        if (!options.Value.EnforcePasswordChange || !IsPanelSession(context) || AuthPath.IsMatch(context.Request.Path.Value ?? string.Empty))
        {
            await next(context);
            return;
        }

        if (!int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await next(context);
            return;
        }

        bool pending;
        try
        {
            pending = await cache.IsPasswordChangePendingAsync(userId, context.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "PasswordChange: no se pudo verificar la cuenta {UserId}; se permite la solicitud (fail-open).",
                userId);
            await next(context);
            return;
        }

        if (!pending)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(
            """{"status":403,"title":"Forbidden","code":"password_change_required","detail":"Debes cambiar tu contraseña temporal antes de continuar."}""");
    }

    // Mismo criterio que el chequeo CSRF de Program.cs: si vino Authorization, el token no
    // salió de la cookie aunque el navegador también la mande.
    private static bool IsPanelSession(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true &&
        !context.Request.Headers.ContainsKey("Authorization") &&
        context.Request.Cookies.ContainsKey(AuthCookieNames.AccessToken);
}
