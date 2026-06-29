using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;

namespace NicaRunner.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request, CancellationToken ct)
    {
        var result = await authService.GoogleLoginAsync(request, ct);
        return Ok(result);
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
}
