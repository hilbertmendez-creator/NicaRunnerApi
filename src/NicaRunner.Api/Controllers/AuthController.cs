using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/auth")]                            // Legacy — clientes actuales
[Route("api/v{version:apiVersion}/auth")]      // Nueva — clientes que optan por versión explícita
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("google-login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request, CancellationToken ct)
    {
        var result = await authService.GoogleLoginAsync(request, ct);
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
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request, ct);
        return Ok(result);
    }

    // Sin [Authorize] por la misma razón: idempotente y opera sobre el refresh
    // token. Devolvemos 204 — no hay payload útil que retornar.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(request, ct);
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
