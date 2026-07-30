using System.Security.Claims;
using System.Security.Cryptography;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NicaRunner.Api.Auth;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/auth")]                            // Legacy — clientes actuales
[Route("api/v{version:apiVersion}/auth")]      // Nueva — clientes que optan por versión explícita
public class AuthController(IAuthService authService, IHostEnvironment environment) : ControllerBase
{
    // El body de las respuestas de abajo sigue devolviendo token/refreshToken
    // en JSON sin cambios — la app Android los lee de ahí y no usa cookies.
    // El cliente web (Vercel) en cambio ignora el body y usa exclusivamente
    // las cookies httpOnly que seteamos acá.
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        SetAuthCookies(result);
        return Ok(result);
    }

    [HttpPost("google-login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request, CancellationToken ct)
    {
        var result = await authService.GoogleLoginAsync(request, ct);
        SetAuthCookies(result);
        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(GetUserId(), request, ct);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await authService.ForgotPasswordAsync(request, ct);
        return Ok();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await authService.ResetPasswordAsync(request, ct);
        return NoContent();
    }

    // Sin [Authorize]: el access token del cliente puede estar expirado al
    // momento de pedir el refresh; el refresh token mismo es la credencial.
    // request es nullable: el cliente web no manda body (el refresh token
    // viaja en la cookie nr_rt); la app Android sigue mandándolo en el body.
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest? request, CancellationToken ct)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies[AuthCookieNames.RefreshToken];
        var result = await authService.RefreshAsync(new RefreshRequest(refreshToken), ct);
        SetAuthCookies(result);
        return Ok(result);
    }

    // Sin [Authorize] por la misma razón: idempotente y opera sobre el refresh
    // token. Devolvemos 204 — no hay payload útil que retornar.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest? request, CancellationToken ct)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies[AuthCookieNames.RefreshToken];
        await authService.LogoutAsync(new LogoutRequest(refreshToken), ct);
        ClearAuthCookies();
        return NoContent();
    }

    // Usado por el cliente web al arrancar para saber si hay sesión activa —
    // no puede leer la cookie httpOnly con JS, así que le pregunta al server.
    // Consulta la BD (no solo los claims del JWT) para traer MustChangePassword
    // actualizado, necesario para no perder el flujo de cambio forzado en un
    // refresh de página a mitad de camino.
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct)
    {
        var result = await authService.GetCurrentUserAsync(GetUserId(), ct);
        return Ok(result);
    }

    private void SetAuthCookies(AuthResponse result)
    {
        var isProd = !environment.IsDevelopment();

        Response.Cookies.Append(AuthCookieNames.AccessToken, result.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = isProd ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            Expires = result.ExpiresAtUtc,
        });

        Response.Cookies.Append(AuthCookieNames.RefreshToken, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = isProd ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            Expires = result.RefreshExpiresAtUtc,
        });

        // No-httpOnly a propósito: en dev el frontend puede leerla con JS vía
        // document.cookie; en prod (dominio distinto) no puede, así que además
        // la exponemos en el header X-CSRF-Token de esta misma respuesta — el
        // eco genérico de Program.cs no alcanza a cubrir este caso porque lee
        // la cookie del request entrante, y acá recién se está rotando.
        var csrfToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append(AuthCookieNames.Csrf, csrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = isProd,
            SameSite = isProd ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            Expires = result.RefreshExpiresAtUtc,
        });
        Response.Headers[AuthCookieNames.CsrfHeader] = csrfToken;
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(AuthCookieNames.AccessToken, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(AuthCookieNames.RefreshToken, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(AuthCookieNames.Csrf, new CookieOptions { Path = "/" });
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
