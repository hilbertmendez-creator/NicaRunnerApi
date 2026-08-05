using NicaRunner.Api.Auth;

namespace NicaRunner.Api.Middleware;

// CSRF (double-submit cookie) para tráfico autenticado por cookie: si el
// browser manda la cookie nr_at o nr_rt en un request mutante SIN header
// Authorization, exige que el header X-CSRF-Token coincida con la cookie
// nr_csrf (legible por JS a propósito). Clientes que sí mandan Authorization
// (Android, scripts, admin) no dependen de cookies ambient y quedan afuera de
// este chequeo — CSRF ataca cookies que el browser adjunta solo.
public class CsrfMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    // Endpoints anónimos que se autentican con credenciales explícitas del body
    // (password, google id token, link de reseteo) y no con la cookie ambient:
    // no dependen de nr_at/nr_rt para autorizar nada, así que no deben exigir
    // CSRF. Sin esto, un usuario con cookies viejas/expiradas de una sesión
    // anterior (y el csrfToken en memoria del cliente perdido por haber recargado
    // la página) queda bloqueado con 403 al intentar loguearse o pedir un reset
    // de contraseña — el propio login/forgot-password es lo que debería dejarlo
    // arrancar de cero.
    private static readonly string[] CsrfExemptPaths =
        { "/auth/login", "/auth/google-login", "/auth/forgot-password", "/auth/reset-password" };

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // Eco de la cookie CSRF como header en toda respuesta donde viaje: el
        // frontend (dominio distinto al de esta API) no puede leerla vía
        // document.cookie, así que se la devolvemos acá para que la guarde en
        // memoria y la reenvíe como X-CSRF-Token en el próximo request mutante.
        if (request.Cookies.TryGetValue(AuthCookieNames.Csrf, out var csrfCookieValue) && !string.IsNullOrEmpty(csrfCookieValue))
            context.Response.Headers[AuthCookieNames.CsrfHeader] = csrfCookieValue;

        var usaCookieAuth = !request.Headers.ContainsKey("Authorization") &&
            (request.Cookies.ContainsKey(AuthCookieNames.AccessToken) || request.Cookies.ContainsKey(AuthCookieNames.RefreshToken));

        var isCsrfExempt = CsrfExemptPaths.Any(path =>
            request.Path.Value?.EndsWith(path, StringComparison.OrdinalIgnoreCase) == true);

        if (MutatingMethods.Contains(request.Method) && usaCookieAuth && !isCsrfExempt)
        {
            var csrfCookie = request.Cookies[AuthCookieNames.Csrf];
            var csrfHeader = request.Headers[AuthCookieNames.CsrfHeader].ToString();
            if (string.IsNullOrEmpty(csrfCookie) || !string.Equals(csrfCookie, csrfHeader, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(
                    """{"status":403,"title":"Forbidden","detail":"CSRF token invalido o ausente."}""");
                return;
            }
        }

        await next(context);
    }
}
