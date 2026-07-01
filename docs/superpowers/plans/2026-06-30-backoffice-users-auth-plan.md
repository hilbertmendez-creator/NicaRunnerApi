# Backoffice Users, Auth & Login Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Seed 3 backoffice admin users with a temporary password, force a password change on first login, add a "forgot password" flow, build a user/role maintenance screen, and redesign the login page with the NicaRunner brand.

**Architecture:** .NET 8 / EF Core (Domain → Application → Infrastructure → Api) backend, React + TypeScript + Tailwind frontend (`@nicarunner/ui` component library). Follows the existing patterns in this repo exactly: services take repository interfaces via primary-constructor DI, controllers are thin, DTOs are records, custom exceptions map to HTTP status codes via `ExceptionHandlingMiddleware`.

**Tech Stack:** ASP.NET Core 8, EF Core 8 (Sqlite dev / Npgsql prod), xUnit + Moq for backend tests, React 19 + react-router-dom 7 + axios + Tailwind 4 for frontend (no frontend test runner in this repo — verify frontend changes by running the app in a browser).

**Spec:** `docs/superpowers/specs/2026-06-30-backoffice-users-auth-design.md`

---

## Before you start

All backend commands below assume your shell's working directory is `src/NicaRunner.Api` (where the startup project's `.csproj` lives). All frontend commands assume `frontend/`.

Run once to confirm the baseline builds and existing tests pass:

```bash
dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj
```
Expected: all existing tests pass (baseline before this plan's changes).

---

### Task 1: Domain model — add password-reset/must-change fields to `User`

**Files:**
- Modify: `src/NicaRunner.Domain/Entities/User.cs`
- Create (via `dotnet ef`): a new migration in `src/NicaRunner.Infrastructure/Migrations/`

- [ ] **Step 1: Add the three new properties to `User`**

Edit `src/NicaRunner.Domain/Entities/User.cs` — replace the whole `User` class body:

```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public AuthProvider Provider { get; set; } = AuthProvider.Local;
    public string Nombre { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
}
```

- [ ] **Step 2: Build to confirm it compiles**

Run (from `src/NicaRunner.Api`): `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Generate the EF Core migration**

Run (from `src/NicaRunner.Api`):
```bash
dotnet ef migrations add AddUserPasswordResetFields --project ../NicaRunner.Infrastructure --startup-project .
```
Expected: a new pair of files under `src/NicaRunner.Infrastructure/Migrations/` (e.g. `<timestamp>_AddUserPasswordResetFields.cs` + `.Designer.cs`) containing `AddColumn` calls for `MustChangePassword` (bool, default `true`), `PasswordResetToken` (nullable string), `PasswordResetTokenExpiry` (nullable datetime) on the `Users` table.

- [ ] **Step 4: Apply the migration to the dev Sqlite database**

Run (from `src/NicaRunner.Api`):
```bash
dotnet ef database update --project ../NicaRunner.Infrastructure --startup-project .
```
Expected: `Done.`

- [ ] **Step 5: Commit**

```bash
git add src/NicaRunner.Domain/Entities/User.cs src/NicaRunner.Infrastructure/Migrations/
git commit -m "feat(domain): agrega MustChangePassword y campos de reset de password a User"
```

---

### Task 2: Let `INotificationSender` carry an optional subject

Password-reset and temporary-password emails need a different subject than the existing "Tu resultado en NicaRunner" default used for race-result notifications. Extending the interface with an optional `subject` parameter keeps the existing call site working with a one-line update.

**Files:**
- Modify: `src/NicaRunner.Application/Common/Interfaces/INotificationSender.cs`
- Modify: `src/NicaRunner.Infrastructure/Notifications/ResendEmailSender.cs`
- Modify: `src/NicaRunner.Infrastructure/Notifications/StubWhatsAppSender.cs`
- Modify: `src/NicaRunner.Application/Notifications/NotificationService.cs`

- [ ] **Step 1: Add the optional `subject` parameter to the interface**

Edit `src/NicaRunner.Application/Common/Interfaces/INotificationSender.cs`:

```csharp
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public record NotificationSendResult(bool Success, string? ErrorMessage);

/// <summary>
/// Punto de extensión por canal. Cuando existan credenciales reales (SendGrid, WhatsApp
/// Business API), se agrega una implementación concreta en Infrastructure y se reemplaza
/// el registro del stub correspondiente en Program.cs — Application y Api no cambian.
/// </summary>
public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task<NotificationSendResult> SendAsync(string destino, string mensaje, string? subject = null, CancellationToken ct = default);
}
```

- [ ] **Step 2: Update `ResendEmailSender` to use the override when provided**

Edit `src/NicaRunner.Infrastructure/Notifications/ResendEmailSender.cs` — replace the `SendAsync` signature and payload construction:

```csharp
    public async Task<NotificationSendResult> SendAsync(string destino, string mensaje, string? subject = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return new NotificationSendResult(
                false,
                "Falta configurar Resend:ApiKey (ver appsettings.Development.json o user-secrets).");
        }

        var payload = new
        {
            from = _options.FromEmail,
            to = new[] { destino },
            subject = subject ?? _options.Subject,
            text = mensaje,
        };
```

(Resto del método sin cambios.)

- [ ] **Step 3: Update `StubWhatsAppSender`**

Edit `src/NicaRunner.Infrastructure/Notifications/StubWhatsAppSender.cs`:

```csharp
    public Task<NotificationSendResult> SendAsync(string destino, string mensaje, string? subject = null, CancellationToken ct = default) =>
        Task.FromResult(new NotificationSendResult(
            false,
            "Integración de WhatsApp pendiente de configurar (proveedor y credenciales)."));
```

- [ ] **Step 4: Fix the one existing call site**

Edit `src/NicaRunner.Application/Notifications/NotificationService.cs:118` — the call currently reads:

```csharp
                var sendResult = await sender.SendAsync(destino, mensaje, ct);
```

Change to a named argument so it still binds `ct` to the cancellation token (not the new `subject` parameter):

```csharp
                var sendResult = await sender.SendAsync(destino, mensaje, ct: ct);
```

- [ ] **Step 5: Build and run existing tests**

Run: `dotnet build` (from `src/NicaRunner.Api`)
Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj`
Expected: build succeeds, all existing tests still pass (this task only changes signatures/wiring, no behavior change for existing callers).

- [ ] **Step 6: Commit**

```bash
git add src/NicaRunner.Application/Common/Interfaces/INotificationSender.cs src/NicaRunner.Infrastructure/Notifications/ResendEmailSender.cs src/NicaRunner.Infrastructure/Notifications/StubWhatsAppSender.cs src/NicaRunner.Application/Notifications/NotificationService.cs
git commit -m "feat(notifications): permite un subject por envio en INotificationSender"
```

---

### Task 3: Extend `IUserRepository`/`UserRepository`

**Files:**
- Modify: `src/NicaRunner.Application/Common/Interfaces/IUserRepository.cs`
- Modify: `src/NicaRunner.Infrastructure/Repositories/UserRepository.cs`

- [ ] **Step 1: Add the three new methods to the interface**

Edit `src/NicaRunner.Application/Common/Interfaces/IUserRepository.cs`:

```csharp
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByResetTokenAsync(string token, CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Implement them in `UserRepository`**

Edit `src/NicaRunner.Infrastructure/Repositories/UserRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class UserRepository(NicaRunnerDbContext context) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByResetTokenAsync(string token, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token, ct);

    public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
        context.Users.OrderBy(u => u.Email).ToListAsync(ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await context.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
```

- [ ] **Step 3: Build**

Run: `dotnet build` (from `src/NicaRunner.Api`)
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/NicaRunner.Application/Common/Interfaces/IUserRepository.cs src/NicaRunner.Infrastructure/Repositories/UserRepository.cs
git commit -m "feat(users): agrega GetByIdAsync, GetByResetTokenAsync y GetAllAsync a IUserRepository"
```

---

### Task 4: `AuthResponse.MustChangePassword`

**Files:**
- Modify: `src/NicaRunner.Application/Auth/Dtos/AuthResponse.cs`
- Modify: `src/NicaRunner.Application/Auth/AuthService.cs`
- Test: `tests/NicaRunner.Tests/AuthServiceGoogleLoginTests.cs` (add one assertion to an existing test)

- [ ] **Step 1: Write/extend the failing test**

`GoogleLoginAsync`'s "new Google user" branch (`AuthService.cs`, `GoogleLoginAsync`) never touches `MustChangePassword`, so a freshly Google-provisioned user keeps the C# default from Task 1 (`true`). This is fine — it's a placeholder value irrelevant for `Provider = Google` users, since they have no local password to force a change on (the frontend's `RequirePasswordChanged` gate in Task 13 only matters for `Provider = Local`/`LocalAndGoogle` users who actually log in with a password). This step just locks in that current behavior with a test.

Edit `tests/NicaRunner.Tests/AuthServiceGoogleLoginTests.cs` — in `GoogleLogin_EmailNuevo_CreaUsuarioGoogle`, add this assertion right before the closing brace of the test method, after the existing `_users.Verify(...)` line:

```csharp
        Assert.True(result.MustChangePassword);
```

- [ ] **Step 2: Run the test to verify it fails (compile error — `MustChangePassword` doesn't exist yet on `AuthResponse`)**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj --filter GoogleLogin_EmailNuevo_CreaUsuarioGoogle`
Expected: build error, `'AuthResponse' does not contain a definition for 'MustChangePassword'`.

- [ ] **Step 3: Add the field to `AuthResponse`**

Edit `src/NicaRunner.Application/Auth/Dtos/AuthResponse.cs`:

```csharp
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth.Dtos;

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    int UserId,
    string Email,
    string Nombre,
    UserRole Role,
    bool MustChangePassword);
```

- [ ] **Step 4: Update `AuthService.BuildAuthResponse` to populate it**

Edit `src/NicaRunner.Application/Auth/AuthService.cs:79-83` — replace `BuildAuthResponse`:

```csharp
    private AuthResponse BuildAuthResponse(User user)
    {
        var generated = jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(generated.Token, generated.ExpiresAtUtc, user.Id, user.Email, user.Nombre, user.Role, user.MustChangePassword);
    }
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj`
Expected: all tests pass, including `GoogleLogin_EmailNuevo_CreaUsuarioGoogle`.

- [ ] **Step 6: Commit**

```bash
git add src/NicaRunner.Application/Auth/Dtos/AuthResponse.cs src/NicaRunner.Application/Auth/AuthService.cs tests/NicaRunner.Tests/AuthServiceGoogleLoginTests.cs
git commit -m "feat(auth): expone MustChangePassword en la respuesta de login"
```

---

### Task 5: Change-password endpoint

**Files:**
- Create: `src/NicaRunner.Application/Auth/Dtos/ChangePasswordRequest.cs`
- Modify: `src/NicaRunner.Application/Auth/IAuthService.cs`
- Modify: `src/NicaRunner.Application/Auth/AuthService.cs`
- Modify: `src/NicaRunner.Api/Controllers/AuthController.cs`
- Test: Create `tests/NicaRunner.Tests/AuthServiceChangePasswordTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/NicaRunner.Tests/AuthServiceChangePasswordTests.cs`:

```csharp
using Moq;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class AuthServiceChangePasswordTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IGoogleAuthService> _google = new();

    private AuthService BuildService() =>
        new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object);

    [Fact]
    public async Task ChangePassword_CurrentPasswordCorrecta_ActualizaHashYLimpiaFlag()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash-vieja", MustChangePassword = true };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("actual", "hash-vieja")).Returns(true);
        _passwordHasher.Setup(p => p.Hash("nueva-segura")).Returns("hash-nueva");

        await BuildService().ChangePasswordAsync(1, new ChangePasswordRequest("actual", "nueva-segura"));

        Assert.Equal("hash-nueva", user.PasswordHash);
        Assert.False(user.MustChangePassword);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_CurrentPasswordIncorrecta_LanzaInvalidCredentials()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash-vieja" };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("mala", "hash-vieja")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ChangePasswordAsync(1, new ChangePasswordRequest("mala", "nueva-segura")));

        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_UsuarioInexistente_LanzaNotFound()
    {
        _users.Setup(u => u.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().ChangePasswordAsync(99, new ChangePasswordRequest("x", "y")));
    }
}
```

- [ ] **Step 2: Run to verify it fails to compile**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj --filter AuthServiceChangePasswordTests`
Expected: compile error — `ChangePasswordAsync`/`ChangePasswordRequest` don't exist yet.

- [ ] **Step 3: Create the DTO**

Create `src/NicaRunner.Application/Auth/Dtos/ChangePasswordRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Auth.Dtos;

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(6)] string NewPassword);
```

- [ ] **Step 4: Add the method to `IAuthService`**

Edit `src/NicaRunner.Application/Auth/IAuthService.cs`:

```csharp
using NicaRunner.Application.Auth.Dtos;

namespace NicaRunner.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
}
```

- [ ] **Step 5: Implement it in `AuthService`**

Edit `src/NicaRunner.Application/Auth/AuthService.cs` — add `using NicaRunner.Application.Common.Exceptions;` if not already present (it already is), then add the method after `GoogleLoginAsync`:

```csharp
    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {userId}.");

        if (user.PasswordHash is null || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidCredentialsException("La contraseña actual no es correcta.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;

        await userRepository.SaveChangesAsync(ct);
    }
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj --filter AuthServiceChangePasswordTests`
Expected: 3 tests pass.

- [ ] **Step 7: Add the controller endpoint**

Edit `src/NicaRunner.Api/Controllers/AuthController.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(GetUserId(), request, ct);
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

- [ ] **Step 8: Build the whole solution**

Run: `dotnet build` (from `src/NicaRunner.Api`)
Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add src/NicaRunner.Application/Auth/Dtos/ChangePasswordRequest.cs src/NicaRunner.Application/Auth/IAuthService.cs src/NicaRunner.Application/Auth/AuthService.cs src/NicaRunner.Api/Controllers/AuthController.cs tests/NicaRunner.Tests/AuthServiceChangePasswordTests.cs
git commit -m "feat(auth): agrega endpoint POST /api/auth/change-password"
```

---

### Task 6: Forgot-password / reset-password flow

**Files:**
- Create: `src/NicaRunner.Application/Auth/Dtos/ForgotPasswordRequest.cs`
- Create: `src/NicaRunner.Application/Auth/Dtos/ResetPasswordRequest.cs`
- Create: `src/NicaRunner.Application/Common/FrontendOptions.cs`
- Modify: `src/NicaRunner.Application/Auth/IAuthService.cs`
- Modify: `src/NicaRunner.Application/Auth/AuthService.cs`
- Modify: `src/NicaRunner.Api/Controllers/AuthController.cs`
- Modify: `src/NicaRunner.Api/Program.cs`
- Modify: `src/NicaRunner.Api/appsettings.json`
- Modify: `src/NicaRunner.Api/appsettings.Development.json`
- Test: Create `tests/NicaRunner.Tests/AuthServicePasswordResetTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/NicaRunner.Tests/AuthServicePasswordResetTests.cs`:

```csharp
using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Application.Auth;
using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class AuthServicePasswordResetTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IGoogleAuthService> _google = new();
    private readonly Mock<INotificationSender> _emailSender = new();
    private readonly IOptions<FrontendOptions> _frontendOptions = Options.Create(new FrontendOptions { BaseUrl = "https://backoffice.nicarunner.test" });

    private AuthService BuildService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        return new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object, [_emailSender.Object], _frontendOptions);
    }

    [Fact]
    public async Task ForgotPassword_EmailExistenteLocal_GeneraTokenYEnviaEmail()
    {
        var user = new User { Id = 1, Email = "a@b.com", Provider = AuthProvider.Local, PasswordHash = "hash" };
        _users.Setup(u => u.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _emailSender.Setup(s => s.SendAsync("a@b.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        await BuildService().ForgotPasswordAsync(new ForgotPasswordRequest("a@b.com"));

        Assert.NotNull(user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetTokenExpiry);
        _emailSender.Verify(s => s.SendAsync("a@b.com", It.Is<string>(m => m.Contains("https://backoffice.nicarunner.test")), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_EmailInexistente_NoLanzaYNoEnviaEmail()
    {
        _users.Setup(u => u.GetByEmailAsync("nadie@b.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await BuildService().ForgotPasswordAsync(new ForgotPasswordRequest("nadie@b.com"));

        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_UsuarioSoloGoogle_NoEnviaEmail()
    {
        var user = new User { Id = 2, Email = "g@b.com", Provider = AuthProvider.Google, PasswordHash = null };
        _users.Setup(u => u.GetByEmailAsync("g@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await BuildService().ForgotPasswordAsync(new ForgotPasswordRequest("g@b.com"));

        Assert.Null(user.PasswordResetToken);
        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_TokenValido_ActualizaPasswordYLimpiaToken()
    {
        var user = new User
        {
            Id = 1,
            Email = "a@b.com",
            PasswordHash = "hash-vieja",
            PasswordResetToken = "token-123",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(10),
            MustChangePassword = true
        };
        _users.Setup(u => u.GetByResetTokenAsync("token-123", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Hash("nueva-segura")).Returns("hash-nueva");

        await BuildService().ResetPasswordAsync(new ResetPasswordRequest("token-123", "nueva-segura"));

        Assert.Equal("hash-nueva", user.PasswordHash);
        Assert.Null(user.PasswordResetToken);
        Assert.Null(user.PasswordResetTokenExpiry);
        Assert.False(user.MustChangePassword);
    }

    [Fact]
    public async Task ResetPassword_TokenExpirado_LanzaInvalidCredentials()
    {
        var user = new User
        {
            Id = 1,
            Email = "a@b.com",
            PasswordResetToken = "token-123",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-1)
        };
        _users.Setup(u => u.GetByResetTokenAsync("token-123", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ResetPasswordAsync(new ResetPasswordRequest("token-123", "nueva-segura")));
    }

    [Fact]
    public async Task ResetPassword_TokenInexistente_LanzaInvalidCredentials()
    {
        _users.Setup(u => u.GetByResetTokenAsync("no-existe", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ResetPasswordAsync(new ResetPasswordRequest("no-existe", "nueva-segura")));
    }
}
```

- [ ] **Step 2: Run to verify it fails to compile**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj --filter AuthServicePasswordResetTests`
Expected: compile errors (missing DTOs, `FrontendOptions`, new `AuthService` constructor params, `ForgotPasswordAsync`/`ResetPasswordAsync`).

- [ ] **Step 3: Create the DTOs and `FrontendOptions`**

Create `src/NicaRunner.Application/Auth/Dtos/ForgotPasswordRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Auth.Dtos;

public record ForgotPasswordRequest([Required, EmailAddress] string Email);
```

Create `src/NicaRunner.Application/Auth/Dtos/ResetPasswordRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Auth.Dtos;

public record ResetPasswordRequest(
    [Required] string Token,
    [Required, MinLength(6)] string NewPassword);
```

Create `src/NicaRunner.Application/Common/FrontendOptions.cs`:

```csharp
namespace NicaRunner.Application.Common;

public class FrontendOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5173";
}
```

- [ ] **Step 4: Add the two methods to `IAuthService`**

Edit `src/NicaRunner.Application/Auth/IAuthService.cs` — add after `ChangePasswordAsync`:

```csharp
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
```

- [ ] **Step 5: Implement them in `AuthService`**

Edit `src/NicaRunner.Application/Auth/AuthService.cs` — replace the whole file:

```csharp
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IGoogleAuthService googleAuthService,
    IEnumerable<INotificationSender> notificationSenders,
    IOptions<FrontendOptions> frontendOptions) : IAuthService
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await userRepository.EmailExistsAsync(request.Email, ct))
            throw new ConflictException($"Ya existe un usuario registrado con el email '{request.Email}'.");

        var user = new User
        {
            Email = request.Email,
            Nombre = request.Nombre,
            Role = request.Role,
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        await userRepository.AddAsync(user, ct);
        await userRepository.SaveChangesAsync(ct);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive || user.PasswordHash is null ||
            !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException("Email o contraseña incorrectos.");

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        var google = await googleAuthService.ValidateIdTokenAsync(request.IdToken, ct);
        if (google is null)
            throw new InvalidCredentialsException("Token de Google inválido o expirado.");

        var user = await userRepository.GetByGoogleIdAsync(google.Sub, ct);
        if (user is null)
        {
            user = await userRepository.GetByEmailAsync(google.Email, ct);
            if (user is null)
            {
                user = new User
                {
                    Email = google.Email,
                    Nombre = google.Nombre,
                    GoogleId = google.Sub,
                    Provider = AuthProvider.Google
                };
                await userRepository.AddAsync(user, ct);
            }
            else
            {
                user.GoogleId = google.Sub;
                user.Provider = user.PasswordHash is null ? AuthProvider.Google : AuthProvider.LocalAndGoogle;
            }
        }

        if (!user.IsActive)
            throw new ForbiddenException("Esta cuenta está desactivada.");

        await userRepository.SaveChangesAsync(ct);

        return BuildAuthResponse(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {userId}.");

        if (user.PasswordHash is null || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidCredentialsException("La contraseña actual no es correcta.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;

        await userRepository.SaveChangesAsync(ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);

        // Nunca revelamos si el email existe o no (evita enumeración de usuarios):
        // si no hay usuario local que resetear, simplemente no hacemos nada.
        if (user is null || user.PasswordHash is null)
            return;

        user.PasswordResetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenExpiry = DateTime.UtcNow.Add(ResetTokenLifetime);
        await userRepository.SaveChangesAsync(ct);

        var resetLink = $"{frontendOptions.Value.BaseUrl}/reset-password?token={user.PasswordResetToken}";
        var mensaje = $"Hola {user.Nombre}, recibimos una solicitud para restablecer tu contraseña de NicaRunner Backoffice. " +
            $"Este link es válido por 30 minutos: {resetLink}\n\nSi no solicitaste esto, ignora este correo.";

        var emailSender = notificationSenders.First(s => s.Channel == NotificationChannel.Email);
        await emailSender.SendAsync(user.Email, mensaje, "Restablece tu contraseña de NicaRunner", ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByResetTokenAsync(request.Token, ct);
        if (user is null || user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidCredentialsException("El link para restablecer la contraseña no es válido o ya expiró.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.MustChangePassword = false;

        await userRepository.SaveChangesAsync(ct);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var generated = jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(generated.Token, generated.ExpiresAtUtc, user.Id, user.Email, user.Nombre, user.Role, user.MustChangePassword);
    }
}
```

- [ ] **Step 6: Fix the two existing test classes' `BuildService()` — `AuthService`'s constructor now takes 6 parameters**

`AuthServiceGoogleLoginTests.cs` (existing) and `AuthServiceChangePasswordTests.cs` (Task 5) both build `AuthService` with the old 4-parameter constructor. Neither of their test scenarios exercises notification-sending or the frontend URL, so an empty sender list and a default `FrontendOptions` are enough to keep them compiling and passing.

Edit `tests/NicaRunner.Tests/AuthServiceGoogleLoginTests.cs` — add `using Microsoft.Extensions.Options;` and `using NicaRunner.Application.Common;` to the top of the file, then replace the `BuildService` method:

```csharp
    private AuthService BuildService() =>
        new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object, [], Options.Create(new FrontendOptions()));
```

Edit `tests/NicaRunner.Tests/AuthServiceChangePasswordTests.cs` — add the same two `using` statements, then replace the `BuildService` method:

```csharp
    private AuthService BuildService() =>
        new(_users.Object, _passwordHasher.Object, _jwt.Object, _google.Object, [], Options.Create(new FrontendOptions()));
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj`
Expected: all tests pass (existing `AuthServiceGoogleLoginTests`, `AuthServiceChangePasswordTests`, and the new `AuthServicePasswordResetTests`).

- [ ] **Step 8: Add the controller endpoints**

Edit `src/NicaRunner.Api/Controllers/AuthController.cs` — add after `ChangePassword`:

```csharp
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await authService.ForgotPasswordAsync(request, ct);
        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await authService.ResetPasswordAsync(request, ct);
        return NoContent();
    }
```

- [ ] **Step 9: Wire `FrontendOptions` in `Program.cs`**

Edit `src/NicaRunner.Api/Program.cs` — add this line right after the existing `builder.Services.Configure<GoogleAuthSettings>(...)` line:

```csharp
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection("Frontend"));
```

Add `using NicaRunner.Application.Common;` to the `using` block at the top of the file.

- [ ] **Step 10: Add the `Frontend` config section**

Edit `src/NicaRunner.Api/appsettings.json` — add a `Frontend` section (keep alphabetical-ish placement next to `GoogleAuth`):

```json
  "Frontend": {
    "BaseUrl": ""
  },
```

Edit `src/NicaRunner.Api/appsettings.Development.json` — add:

```json
  "Frontend": {
    "BaseUrl": "http://localhost:5173"
  },
```

- [ ] **Step 11: Build the whole solution**

Run: `dotnet build` (from `src/NicaRunner.Api`)
Expected: `Build succeeded.`

- [ ] **Step 12: Commit**

```bash
git add src/NicaRunner.Application/Auth/Dtos/ForgotPasswordRequest.cs src/NicaRunner.Application/Auth/Dtos/ResetPasswordRequest.cs src/NicaRunner.Application/Common/FrontendOptions.cs src/NicaRunner.Application/Auth/IAuthService.cs src/NicaRunner.Application/Auth/AuthService.cs src/NicaRunner.Api/Controllers/AuthController.cs src/NicaRunner.Api/Program.cs src/NicaRunner.Api/appsettings.json src/NicaRunner.Api/appsettings.Development.json tests/NicaRunner.Tests/AuthServicePasswordResetTests.cs tests/NicaRunner.Tests/AuthServiceGoogleLoginTests.cs tests/NicaRunner.Tests/AuthServiceChangePasswordTests.cs
git commit -m "feat(auth): agrega flujo de recuperar/restablecer contraseña"
```

---

### Task 7: Seed the 3 admin users at startup

**Files:**
- Create: `src/NicaRunner.Infrastructure/Seed/AdminUserSeeder.cs`
- Modify: `src/NicaRunner.Api/Program.cs`
- Modify: `src/NicaRunner.Api/appsettings.json`
- Modify: `tests/NicaRunner.Tests/NicaRunner.Tests.csproj` (add project reference + Sqlite package so we can test against a real `DbContext`)
- Test: Create `tests/NicaRunner.Tests/AdminUserSeederTests.cs`

- [ ] **Step 1: Add the Infrastructure project reference and Sqlite package to the test project**

Edit `tests/NicaRunner.Tests/NicaRunner.Tests.csproj` — add inside the existing `PackageReference` `ItemGroup`:

```xml
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.*" />
```

Add inside the existing `ProjectReference` `ItemGroup`:

```xml
    <ProjectReference Include="..\..\src\NicaRunner.Infrastructure\NicaRunner.Infrastructure.csproj" />
```

- [ ] **Step 2: Write the failing tests**

Create `tests/NicaRunner.Tests/AdminUserSeederTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Security;
using NicaRunner.Infrastructure.Seed;

namespace NicaRunner.Tests;

public class AdminUserSeederTests
{
    private static NicaRunnerDbContext BuildInMemoryDbContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<NicaRunnerDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new NicaRunnerDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task SeedAsync_BdVacia_CreaLosTresAdministradores()
    {
        using var db = BuildInMemoryDbContext();
        IPasswordHasher hasher = new PasswordHasher();

        await AdminUserSeeder.SeedAsync(db, hasher, "temporal123");

        var admins = await db.Users.ToListAsync();
        Assert.Equal(3, admins.Count);
        Assert.All(admins, u => Assert.Equal(UserRole.Administrador, u.Role));
        Assert.All(admins, u => Assert.True(u.MustChangePassword));
        Assert.Contains(admins, u => u.Email == "hilbert.mendez@gmail.com");
        Assert.Contains(admins, u => u.Email == "evr86.skip@gmail.com");
        Assert.Contains(admins, u => u.Email == "edufisica@ymail.com");
    }

    [Fact]
    public async Task SeedAsync_EjecutadoDosVeces_NoDuplicaUsuarios()
    {
        using var db = BuildInMemoryDbContext();
        IPasswordHasher hasher = new PasswordHasher();

        await AdminUserSeeder.SeedAsync(db, hasher, "temporal123");
        await AdminUserSeeder.SeedAsync(db, hasher, "temporal123");

        Assert.Equal(3, await db.Users.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_UsuarioYaExiste_NoLoSobreescribe()
    {
        using var db = BuildInMemoryDbContext();
        IPasswordHasher hasher = new PasswordHasher();
        db.Users.Add(new User
        {
            Email = "hilbert.mendez@gmail.com",
            Nombre = "Hilbert (ya editado)",
            Role = UserRole.Administrador,
            PasswordHash = hasher.Hash("password-personalizada"),
            MustChangePassword = false
        });
        await db.SaveChangesAsync();

        await AdminUserSeeder.SeedAsync(db, hasher, "temporal123");

        var existing = await db.Users.SingleAsync(u => u.Email == "hilbert.mendez@gmail.com");
        Assert.Equal("Hilbert (ya editado)", existing.Nombre);
        Assert.False(existing.MustChangePassword);
        Assert.Equal(3, await db.Users.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_SinPasswordConfigurada_NoCreaUsuarios()
    {
        using var db = BuildInMemoryDbContext();
        IPasswordHasher hasher = new PasswordHasher();

        await AdminUserSeeder.SeedAsync(db, hasher, defaultPassword: null);

        Assert.Equal(0, await db.Users.CountAsync());
    }
}
```

- [ ] **Step 3: Run to verify it fails to compile**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj --filter AdminUserSeederTests`
Expected: compile error — `AdminUserSeeder` doesn't exist yet.

- [ ] **Step 4: Implement `AdminUserSeeder`**

Create `src/NicaRunner.Infrastructure/Seed/AdminUserSeeder.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Seed;

/// <summary>
/// Crea los administradores de backoffice iniciales si todavía no existen.
/// Seguro de re-ejecutar en cada deploy: solo agrega los que falten, nunca
/// sobreescribe un usuario ya existente (por si ya cambió su password/nombre).
/// </summary>
public static class AdminUserSeeder
{
    private static readonly string[] AdminEmails =
    [
        "hilbert.mendez@gmail.com",
        "evr86.skip@gmail.com",
        "edufisica@ymail.com"
    ];

    public static async Task SeedAsync(NicaRunnerDbContext db, IPasswordHasher passwordHasher, string? defaultPassword, CancellationToken ct = default)
    {
        // Sin password configurada (Seed:DefaultAdminPassword) no hay nada seguro que
        // hashear — se omite el seed en vez de arrancar con una contraseña conocida.
        if (string.IsNullOrWhiteSpace(defaultPassword))
            return;

        foreach (var email in AdminEmails)
        {
            var exists = await db.Users.AnyAsync(u => u.Email == email, ct);
            if (exists)
                continue;

            db.Users.Add(new User
            {
                Email = email,
                Nombre = email,
                Role = UserRole.Administrador,
                Provider = AuthProvider.Local,
                PasswordHash = passwordHasher.Hash(defaultPassword),
                MustChangePassword = true,
                IsActive = true
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj --filter AdminUserSeederTests`
Expected: 4 tests pass.

- [ ] **Step 6: Wire the seeder into `Program.cs`**

Edit `src/NicaRunner.Api/Program.cs` — replace the migration block:

```csharp
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
    db.Database.Migrate();
}
```

with:

```csharp
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
    db.Database.Migrate();
}

// Seed idempotente de administradores de backoffice — corre en ambos entornos:
// en prod para poblar la BD real (una sola vez), en dev para poder probar el
// login localmente. Sin Seed:DefaultAdminPassword configurada, no hace nada.
using (var seedScope = app.Services.CreateScope())
{
    var seedDb = seedScope.ServiceProvider.GetRequiredService<NicaRunnerDbContext>();
    var seedPasswordHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var defaultAdminPassword = builder.Configuration["Seed:DefaultAdminPassword"];
    await AdminUserSeeder.SeedAsync(seedDb, seedPasswordHasher, defaultAdminPassword);
}
```

Add `using NicaRunner.Infrastructure.Seed;` to the `using` block at the top of `Program.cs`.

- [ ] **Step 7: Add the `Seed` config section**

Edit `src/NicaRunner.Api/appsettings.json` — add:

```json
  "Seed": {
    "DefaultAdminPassword": ""
  },
```

(Deliberately left blank in both `appsettings.json` and `appsettings.Development.json` — same pattern as `Resend:ApiKey`. Set it via `dotnet user-secrets set Seed:DefaultAdminPassword "<valor>"` locally, or the `Seed__DefaultAdminPassword` environment variable in Render, and share that value with the 3 admins out-of-band.)

- [ ] **Step 8: Build the whole solution**

Run: `dotnet build` (from `src/NicaRunner.Api`)
Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add tests/NicaRunner.Tests/NicaRunner.Tests.csproj src/NicaRunner.Infrastructure/Seed/AdminUserSeeder.cs tests/NicaRunner.Tests/AdminUserSeederTests.cs src/NicaRunner.Api/Program.cs src/NicaRunner.Api/appsettings.json
git commit -m "feat(seed): crea los 3 administradores de backoffice al arrancar la app"
```

---

### Task 8: User management service (list / create / update role & active state)

**Files:**
- Create: `src/NicaRunner.Application/Users/Dtos/UserDto.cs`
- Create: `src/NicaRunner.Application/Users/Dtos/CreateUserRequest.cs`
- Create: `src/NicaRunner.Application/Users/Dtos/UpdateUserRequest.cs`
- Create: `src/NicaRunner.Application/Users/IUserManagementService.cs`
- Create: `src/NicaRunner.Application/Users/UserManagementService.cs`
- Test: Create `tests/NicaRunner.Tests/UserManagementServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/NicaRunner.Tests/UserManagementServiceTests.cs`:

```csharp
using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Users;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<INotificationSender> _emailSender = new();

    private UserManagementService BuildService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        return new(_users.Object, _passwordHasher.Object, [_emailSender.Object]);
    }

    [Fact]
    public async Task GetAllAsync_DevuelveTodosMapeadosADto()
    {
        _users.Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new User { Id = 1, Email = "a@b.com", Nombre = "A", Role = UserRole.Administrador, IsActive = true },
            new User { Id = 2, Email = "c@d.com", Nombre = "C", Role = UserRole.Lector, IsActive = false },
        ]);

        var result = await BuildService().GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("a@b.com", result[0].Email);
        Assert.False(result[1].IsActive);
    }

    [Fact]
    public async Task CreateAsync_EmailNuevo_CreaUsuarioConPasswordTemporalYEnviaEmail()
    {
        _users.Setup(u => u.EmailExistsAsync("nuevo@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash-temporal");
        _emailSender.Setup(s => s.SendAsync("nuevo@b.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        User? created = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => created = u)
            .Returns(Task.CompletedTask);

        var dto = await BuildService().CreateAsync(new CreateUserRequest("nuevo@b.com", "Nuevo", UserRole.Capturista));

        Assert.NotNull(created);
        Assert.Equal("hash-temporal", created!.PasswordHash);
        Assert.True(created.MustChangePassword);
        Assert.Equal(AuthProvider.Local, created.Provider);
        Assert.Equal("nuevo@b.com", dto.Email);
        _emailSender.Verify(s => s.SendAsync("nuevo@b.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmailYaExiste_LanzaConflict()
    {
        _users.Setup(u => u.EmailExistsAsync("existe@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => BuildService().CreateAsync(new CreateUserRequest("existe@b.com", "X", UserRole.Lector)));
    }

    [Fact]
    public async Task UpdateAsync_CambiaRolYEstadoDeOtroUsuario()
    {
        var target = new User { Id = 2, Email = "b@b.com", Role = UserRole.Capturista, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        var dto = await BuildService().UpdateAsync(currentUserId: 1, targetUserId: 2, new UpdateUserRequest(UserRole.Lector, false));

        Assert.Equal(UserRole.Lector, target.Role);
        Assert.False(target.IsActive);
        Assert.Equal(UserRole.Lector, dto.Role);
    }

    [Fact]
    public async Task UpdateAsync_AdminIntentaDesactivarseASiMismo_LanzaForbidden()
    {
        var self = new User { Id = 1, Email = "a@b.com", Role = UserRole.Administrador, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(self);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 1, new UpdateUserRequest(null, false)));
    }

    [Fact]
    public async Task UpdateAsync_AdminIntentaCambiarSuPropioRol_LanzaForbidden()
    {
        var self = new User { Id = 1, Email = "a@b.com", Role = UserRole.Administrador, IsActive = true };
        _users.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(self);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 1, new UpdateUserRequest(UserRole.Lector, null)));
    }

    [Fact]
    public async Task UpdateAsync_UsuarioInexistente_LanzaNotFound()
    {
        _users.Setup(u => u.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => BuildService().UpdateAsync(currentUserId: 1, targetUserId: 99, new UpdateUserRequest(UserRole.Lector, null)));
    }
}
```

- [ ] **Step 2: Run to verify it fails to compile**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj --filter UserManagementServiceTests`
Expected: compile error — none of these types exist yet.

- [ ] **Step 3: Create the DTOs**

Create `src/NicaRunner.Application/Users/Dtos/UserDto.cs`:

```csharp
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

public record UserDto(
    int Id,
    string Email,
    string Nombre,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);
```

Create `src/NicaRunner.Application/Users/Dtos/CreateUserRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

public record CreateUserRequest(
    [Required, EmailAddress] string Email,
    [Required] string Nombre,
    [Required] UserRole Role);
```

Create `src/NicaRunner.Application/Users/Dtos/UpdateUserRequest.cs`:

```csharp
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

public record UpdateUserRequest(UserRole? Role, bool? IsActive);
```

- [ ] **Step 4: Create the service interface**

Create `src/NicaRunner.Application/Users/IUserManagementService.cs`:

```csharp
using NicaRunner.Application.Users.Dtos;

namespace NicaRunner.Application.Users;

public interface IUserManagementService
{
    Task<List<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(int currentUserId, int targetUserId, UpdateUserRequest request, CancellationToken ct = default);
}
```

- [ ] **Step 5: Implement the service**

Create `src/NicaRunner.Application/Users/UserManagementService.cs`:

```csharp
using System.Security.Cryptography;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users;

public class UserManagementService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEnumerable<INotificationSender> notificationSenders) : IUserManagementService
{
    private const string TempPasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await userRepository.GetAllAsync(ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await userRepository.EmailExistsAsync(request.Email, ct))
            throw new ConflictException($"Ya existe un usuario registrado con el email '{request.Email}'.");

        var tempPassword = GenerateTempPassword();
        var user = new User
        {
            Email = request.Email,
            Nombre = request.Nombre,
            Role = request.Role,
            Provider = AuthProvider.Local,
            PasswordHash = passwordHasher.Hash(tempPassword),
            MustChangePassword = true,
            IsActive = true
        };

        await userRepository.AddAsync(user, ct);
        await userRepository.SaveChangesAsync(ct);

        var mensaje = $"Hola {user.Nombre}, se creó tu cuenta en NicaRunner Backoffice. " +
            $"Tu contraseña temporal es: {tempPassword}\n\nDeberás cambiarla al iniciar sesión por primera vez.";
        var emailSender = notificationSenders.First(s => s.Channel == NotificationChannel.Email);
        await emailSender.SendAsync(user.Email, mensaje, "Tu cuenta en NicaRunner Backoffice", ct);

        return ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(int currentUserId, int targetUserId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(targetUserId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {targetUserId}.");

        if (targetUserId == currentUserId)
        {
            if (request.IsActive is false)
                throw new ForbiddenException("No puedes desactivar tu propia cuenta.");
            if (request.Role is not null && request.Role != user.Role)
                throw new ForbiddenException("No puedes cambiar tu propio rol.");
        }

        if (request.Role is { } role)
            user.Role = role;
        if (request.IsActive is { } isActive)
            user.IsActive = isActive;

        await userRepository.SaveChangesAsync(ct);
        return ToDto(user);
    }

    private static string GenerateTempPassword() =>
        new(RandomNumberGenerator.GetItems<char>(TempPasswordAlphabet, 12));

    private static UserDto ToDto(User user) => new(
        user.Id,
        user.Email,
        user.Nombre,
        user.Role,
        user.IsActive,
        user.CreatedAt);
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj --filter UserManagementServiceTests`
Expected: 7 tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/NicaRunner.Application/Users/ tests/NicaRunner.Tests/UserManagementServiceTests.cs
git commit -m "feat(users): agrega UserManagementService para listar/crear/actualizar usuarios de backoffice"
```

---

### Task 9: `UsersController` + DI wiring

**Files:**
- Create: `src/NicaRunner.Api/Controllers/UsersController.cs`
- Modify: `src/NicaRunner.Api/Program.cs`

- [ ] **Step 1: Create the controller**

Create `src/NicaRunner.Api/Controllers/UsersController.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Users;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class UsersController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken ct) =>
        Ok(await userManagementService.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await userManagementService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), user);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await userManagementService.UpdateAsync(GetUserId(), id, request, ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

- [ ] **Step 2: Register the service in `Program.cs`**

Edit `src/NicaRunner.Api/Program.cs` — add `using NicaRunner.Application.Users;` to the `using` block, and add this line right after `builder.Services.AddScoped<IAuthService, AuthService>();`:

```csharp
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
```

- [ ] **Step 3: Build**

Run: `dotnet build` (from `src/NicaRunner.Api`)
Expected: `Build succeeded.`

- [ ] **Step 4: Manual smoke test via Swagger**

Run: `dotnet run` (from `src/NicaRunner.Api`, dev environment, Sqlite)
Open `http://localhost:<port>/swagger` in a browser and confirm `POST /api/auth/login`, `POST /api/auth/change-password`, `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`, `GET /api/users`, `POST /api/users`, `PATCH /api/users/{id}` all appear with the expected request/response shapes. Stop the server (Ctrl+C) when done.

- [ ] **Step 5: Commit**

```bash
git add src/NicaRunner.Api/Controllers/UsersController.cs src/NicaRunner.Api/Program.cs
git commit -m "feat(users): agrega UsersController para mantenimiento de usuarios de backoffice"
```

---

### Task 10: Frontend types — `types.ts`

**Files:**
- Modify: `frontend/src/api/types.ts`

- [ ] **Step 1: Extend `AuthResponse` and add the new request/DTO types**

Edit `frontend/src/api/types.ts` — replace the `AuthResponse` interface:

```typescript
export interface AuthResponse {
  token: string
  expiresAtUtc: string
  userId: number
  email: string
  nombre: string
  role: UserRole
  mustChangePassword: boolean
}
```

Add these new interfaces right after `AuthResponse` (before `RaceStatus`):

```typescript
export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}

export interface UserDto {
  id: number
  email: string
  nombre: string
  role: UserRole
  isActive: boolean
  createdAt: string
}

export interface CreateUserRequest {
  email: string
  nombre: string
  role: UserRole
}

export interface UpdateUserRequest {
  role?: UserRole
  isActive?: boolean
}
```

- [ ] **Step 2: Type-check**

Run (from `frontend/`): `npx tsc -b --noEmit`
Expected: no new type errors (existing `AuthContext.tsx` construction of `CurrentUser` doesn't read `mustChangePassword` yet — that's fixed in Task 12 — so this step is purely about `types.ts` itself compiling cleanly, which it will since it has no dependencies).

- [ ] **Step 3: Commit**

```bash
git add frontend/src/api/types.ts
git commit -m "feat(frontend): agrega tipos para cambio/recuperacion de password y mantenimiento de usuarios"
```

---

### Task 11: Frontend API calls — `endpoints.ts`

**Files:**
- Modify: `frontend/src/api/endpoints.ts`

- [ ] **Step 1: Add the imports and the new functions**

Edit `frontend/src/api/endpoints.ts` — add to the `import type { ... } from './types'` block (keep the list alphabetized like the rest):

```typescript
  ChangePasswordRequest,
  CreateUserRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  UpdateUserRequest,
  UserDto,
```

Add these functions at the end of the file:

```typescript
export async function changePassword(request: ChangePasswordRequest): Promise<void> {
  await apiClient.post('/auth/change-password', request)
}

export async function forgotPassword(request: ForgotPasswordRequest): Promise<void> {
  await apiClient.post('/auth/forgot-password', request)
}

export async function resetPassword(request: ResetPasswordRequest): Promise<void> {
  await apiClient.post('/auth/reset-password', request)
}

export async function getUsers(): Promise<UserDto[]> {
  const { data } = await apiClient.get<UserDto[]>('/users')
  return data
}

export async function createUser(request: CreateUserRequest): Promise<UserDto> {
  const { data } = await apiClient.post<UserDto>('/users', request)
  return data
}

export async function updateUser(id: number, request: UpdateUserRequest): Promise<UserDto> {
  const { data } = await apiClient.patch<UserDto>(`/users/${id}`, request)
  return data
}
```

- [ ] **Step 2: Type-check**

Run (from `frontend/`): `npx tsc -b --noEmit`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/api/endpoints.ts
git commit -m "feat(frontend): agrega llamadas API para password y mantenimiento de usuarios"
```

---

### Task 12: `AuthContext` — track `mustChangePassword`

**Files:**
- Modify: `frontend/src/auth/auth-context.ts`
- Modify: `frontend/src/auth/AuthContext.tsx`

- [ ] **Step 1: Add `mustChangePassword` to `CurrentUser` and expose a setter**

Edit `frontend/src/auth/auth-context.ts`:

```typescript
import { createContext, useContext } from 'react'
import type { UserRole } from '../api/types'

export interface CurrentUser {
  userId: number
  email: string
  nombre: string
  role: UserRole
  mustChangePassword: boolean
}

export interface AuthContextValue {
  user: CurrentUser | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  clearMustChangePassword: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth debe usarse dentro de AuthProvider')
  return ctx
}
```

- [ ] **Step 2: Populate it on login and add `clearMustChangePassword`**

Edit `frontend/src/auth/AuthContext.tsx`:

```typescript
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { login as loginRequest } from '../api/endpoints'
import { getStoredToken, setStoredToken, setUnauthorizedHandler } from '../api/client'
import { AuthContext, type AuthContextValue, type CurrentUser } from './auth-context'

const USER_STORAGE_KEY = 'nicarunner.user'

function readStoredUser(): CurrentUser | null {
  const raw = localStorage.getItem(USER_STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as CurrentUser
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(() =>
    getStoredToken() ? readStoredUser() : null,
  )

  const logout = useCallback(() => {
    setStoredToken(null)
    localStorage.removeItem(USER_STORAGE_KEY)
    setUser(null)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(logout)
  }, [logout])

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest(email, password)
    setStoredToken(response.token)
    const currentUser: CurrentUser = {
      userId: response.userId,
      email: response.email,
      nombre: response.nombre,
      role: response.role,
      mustChangePassword: response.mustChangePassword,
    }
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(currentUser))
    setUser(currentUser)
  }, [])

  const clearMustChangePassword = useCallback(() => {
    setUser((current) => {
      if (!current) return current
      const updated = { ...current, mustChangePassword: false }
      localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(updated))
      return updated
    })
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, login, logout, clearMustChangePassword }),
    [user, login, logout, clearMustChangePassword],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
```

- [ ] **Step 2: Type-check**

Run (from `frontend/`): `npx tsc -b --noEmit`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/auth/auth-context.ts frontend/src/auth/AuthContext.tsx
git commit -m "feat(frontend): AuthContext rastrea mustChangePassword tras el login"
```

---

### Task 13: Forced change-password page + routing gate

**Files:**
- Create: `frontend/src/routes/ChangePasswordPage.tsx`
- Create: `frontend/src/auth/RequirePasswordChanged.tsx`
- Modify: `frontend/src/App.tsx`

- [ ] **Step 1: Create the change-password page**

Create `frontend/src/routes/ChangePasswordPage.tsx`:

```tsx
import { useState, type FormEvent } from 'react'
import { changePassword } from '../api/endpoints'
import { useAuth } from '../auth/auth-context'
import { Button, Label, Input } from '@nicarunner/ui'

export function ChangePasswordPage() {
  const { clearMustChangePassword, logout } = useAuth()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (newPassword !== confirmPassword) {
      setError('Las contraseñas nuevas no coinciden.')
      return
    }
    if (newPassword.length < 6) {
      setError('La nueva contraseña debe tener al menos 6 caracteres.')
      return
    }

    setSubmitting(true)
    try {
      await changePassword({ currentPassword, newPassword })
      clearMustChangePassword()
    } catch {
      setError('No se pudo cambiar la contraseña. Verifica la contraseña actual.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-lg bg-white p-8 shadow-md">
        <h1 className="mb-2 text-xl font-semibold text-gray-900">Cambia tu contraseña</h1>
        <p className="mb-6 text-sm text-gray-600">
          Es tu primer inicio de sesión. Define una contraseña personal antes de continuar.
        </p>

        <Label htmlFor="current-password">Contraseña temporal</Label>
        <Input
          id="current-password"
          type="password"
          required
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
          className="mb-4 w-full"
        />

        <Label htmlFor="new-password">Nueva contraseña</Label>
        <Input
          id="new-password"
          type="password"
          required
          minLength={6}
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          className="mb-4 w-full"
        />

        <Label htmlFor="confirm-password">Confirmar nueva contraseña</Label>
        <Input
          id="confirm-password"
          type="password"
          required
          minLength={6}
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          className="mb-4 w-full"
        />

        {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

        <Button type="submit" variant="primary" disabled={submitting} className="mb-3 w-full">
          {submitting ? 'Guardando...' : 'Cambiar contraseña'}
        </Button>
        <button type="button" onClick={logout} className="w-full text-sm text-blue-700 hover:underline">
          Cerrar sesión
        </button>
      </form>
    </div>
  )
}
```

- [ ] **Step 2: Create the routing gate component**

Create `frontend/src/auth/RequirePasswordChanged.tsx`:

```tsx
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './auth-context'

/**
 * Bloquea el resto del backoffice hasta que el usuario complete el cambio
 * de contraseña forzado (seed inicial o password temporal de un admin nuevo).
 */
export function RequirePasswordChanged() {
  const { user } = useAuth()

  if (user?.mustChangePassword) {
    return <Navigate to="/change-password" replace />
  }

  return <Outlet />
}
```

- [ ] **Step 3: Wire it into `App.tsx`**

Edit `frontend/src/App.tsx` — add the import and wrap the existing protected routes with the new gate:

```tsx
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { RequirePasswordChanged } from './auth/RequirePasswordChanged'
import { AppLayout } from './components/AppLayout'
import { LoginPage } from './routes/LoginPage'
import { ChangePasswordPage } from './routes/ChangePasswordPage'
import { DashboardPage } from './features/dashboard/DashboardPage'
import { ResultsPage } from './features/results/ResultsPage'
import { RacesPage } from './features/races/RacesPage'
import { RaceDetailPage } from './features/races/RaceDetailPage'
import { NotificationsPage } from './features/notifications/NotificationsPage'
import { PublicLinksPage } from './features/public-links/PublicLinksPage'
import { PublicResultsPage } from './features/public-results/PublicResultsPage'

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/resultados/:token" element={<PublicResultsPage />} />

          <Route element={<ProtectedRoute allowedRoles={['Administrador', 'Lector']} />}>
            <Route path="/change-password" element={<ChangePasswordPage />} />

            <Route element={<RequirePasswordChanged />}>
              <Route element={<AppLayout />}>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/carreras" element={<RacesPage />} />
                <Route path="/carreras/:raceId" element={<RaceDetailPage />} />
                <Route path="/resultados" element={<ResultsPage />} />
                <Route path="/notificaciones" element={<NotificationsPage />} />
                <Route path="/enlaces" element={<PublicLinksPage />} />
              </Route>
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
```

(The `/usuarios` route and its nav entry are added in Task 17, once the page exists. The `/forgot-password` and `/reset-password` public routes are added in Tasks 15–16.)

- [ ] **Step 4: Type-check**

Run (from `frontend/`): `npx tsc -b --noEmit`
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/routes/ChangePasswordPage.tsx frontend/src/auth/RequirePasswordChanged.tsx frontend/src/App.tsx
git commit -m "feat(frontend): fuerza cambio de password en el primer login"
```

---

### Task 14: Login page redesign (brand panel + logo)

**Files:**
- Create: `frontend/src/routes/NicaRunnerLogo.tsx`
- Modify: `frontend/src/routes/LoginPage.tsx`

- [ ] **Step 1: Extract the brand mark as a reusable component**

Create `frontend/src/routes/NicaRunnerLogo.tsx` (inlined copy of `frontend/public/favicon.svg`'s path data, sized via `className`):

```tsx
export function NicaRunnerLogo({ className = 'h-16 w-16' }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 48 46"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      role="img"
      aria-label="Logo de NicaRunner"
    >
      <path
        fill="#863bff"
        d="M25.946 44.938c-.664.845-2.021.375-2.021-.698V33.937a2.26 2.26 0 0 0-2.262-2.262H10.287c-.92 0-1.456-1.04-.92-1.788l7.48-10.471c1.07-1.497 0-3.578-1.842-3.578H1.237c-.92 0-1.456-1.04-.92-1.788L10.013.474c.214-.297.556-.474.92-.474h28.894c.92 0 1.456 1.04.92 1.788l-7.48 10.471c-1.07 1.498 0 3.579 1.842 3.579h11.377c.943 0 1.473 1.088.89 1.83L25.947 44.94z"
      />
    </svg>
  )
}
```

(Se usa solo la silueta principal del logo, sin los blurs/gradientes decorativos internos del favicon original — a este tamaño y contexto de marca esos detalles no se perciben y solo agregan peso al SVG.)

- [ ] **Step 2: Redesign `LoginPage.tsx`**

Edit `frontend/src/routes/LoginPage.tsx` — replace the whole file:

```tsx
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'
import { Button, Label, Input } from '@nicarunner/ui'
import { NicaRunnerLogo } from './NicaRunnerLogo'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(email, password)
      navigate('/', { replace: true })
    } catch {
      setError('Email o contraseña incorrectos')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen bg-white lg:flex-row">
      <div className="relative hidden w-1/2 items-center justify-center overflow-hidden bg-slate-blue-900 lg:flex">
        <div
          className="absolute inset-0"
          style={{
            background: 'radial-gradient(circle at 50% 45%, rgba(126,20,255,0.35), transparent 60%)',
          }}
          aria-hidden="true"
        />
        <div className="relative flex flex-col items-center gap-4 text-center">
          <NicaRunnerLogo className="h-36 w-36" />
          <span className="text-2xl font-semibold text-white">nicaRunner</span>
          <span className="text-sm text-zinc-400">Back Office de administración de carreras</span>
        </div>
      </div>

      <div className="flex w-full items-center justify-center px-6 py-12 lg:w-1/2">
        <form onSubmit={handleSubmit} className="w-full max-w-sm">
          <div className="mb-8 flex flex-col items-center gap-2 lg:hidden">
            <NicaRunnerLogo className="h-16 w-16" />
            <span className="text-lg font-semibold text-slate-blue-900">nicaRunner</span>
          </div>

          <h1 className="mb-6 text-xl font-semibold text-gray-900">Back Office</h1>

          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="mb-4 w-full"
          />

          <Label htmlFor="password">Contraseña</Label>
          <Input
            id="password"
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full"
          />
          <div className="mb-4 mt-1 text-right">
            <Link to="/forgot-password" className="text-sm text-blue-700 hover:underline">
              ¿Olvidaste tu contraseña?
            </Link>
          </div>

          {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

          <Button type="submit" variant="primary" disabled={submitting} className="w-full">
            {submitting ? 'Ingresando...' : 'Ingresar'}
          </Button>
        </form>
      </div>
    </div>
  )
}
```

- [ ] **Step 3: Type-check**

Run (from `frontend/`): `npx tsc -b --noEmit`
Expected: no errors (`/forgot-password` route doesn't exist yet — that's fine, `react-router-dom`'s `Link` doesn't validate routes at compile time; the route is added in Task 15).

- [ ] **Step 4: Commit**

```bash
git add frontend/src/routes/NicaRunnerLogo.tsx frontend/src/routes/LoginPage.tsx
git commit -m "feat(frontend): rediseña la pantalla de login con el logo de nicaRunner"
```

---

### Task 15: `ForgotPasswordPage`

**Files:**
- Create: `frontend/src/routes/ForgotPasswordPage.tsx`
- Modify: `frontend/src/App.tsx`

- [ ] **Step 1: Create the page**

Create `frontend/src/routes/ForgotPasswordPage.tsx`:

```tsx
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { forgotPassword } from '../api/endpoints'
import { Button, Label, Input } from '@nicarunner/ui'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    try {
      await forgotPassword({ email })
    } finally {
      // Siempre mostramos el mismo mensaje, exista o no la cuenta
      // (evita revelar qué correos están registrados).
      setSubmitting(false)
      setSubmitted(true)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <div className="w-full max-w-sm rounded-lg bg-white p-8 shadow-md">
        <h1 className="mb-2 text-xl font-semibold text-gray-900">Recuperar contraseña</h1>

        {submitted ? (
          <>
            <p className="mb-6 text-sm text-gray-700">
              Si el correo <strong>{email}</strong> está registrado, enviamos un enlace para
              restablecer la contraseña. Revisa tu bandeja de entrada.
            </p>
            <Link to="/login" className="text-sm text-blue-700 hover:underline">
              Volver al login
            </Link>
          </>
        ) : (
          <form onSubmit={handleSubmit}>
            <p className="mb-4 text-sm text-gray-600">
              Ingresa tu correo y te enviaremos un enlace para restablecer tu contraseña.
            </p>

            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mb-4 w-full"
            />

            <Button type="submit" variant="primary" disabled={submitting} className="mb-3 w-full">
              {submitting ? 'Enviando...' : 'Enviar enlace'}
            </Button>
            <Link to="/login" className="block text-center text-sm text-blue-700 hover:underline">
              Volver al login
            </Link>
          </form>
        )}
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Add the public route**

Edit `frontend/src/App.tsx` — add the import and the route (right after `/login`):

```tsx
import { ForgotPasswordPage } from './routes/ForgotPasswordPage'
```

```tsx
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
```

- [ ] **Step 3: Type-check**

Run (from `frontend/`): `npx tsc -b --noEmit`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/routes/ForgotPasswordPage.tsx frontend/src/App.tsx
git commit -m "feat(frontend): agrega pantalla de recuperar contraseña"
```

---

### Task 16: `ResetPasswordPage`

**Files:**
- Create: `frontend/src/routes/ResetPasswordPage.tsx`
- Modify: `frontend/src/App.tsx`

- [ ] **Step 1: Create the page**

Create `frontend/src/routes/ResetPasswordPage.tsx`:

```tsx
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { resetPassword } from '../api/endpoints'
import { Button, Label, Input } from '@nicarunner/ui'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (newPassword !== confirmPassword) {
      setError('Las contraseñas no coinciden.')
      return
    }

    setSubmitting(true)
    try {
      await resetPassword({ token, newPassword })
      navigate('/login', { replace: true })
    } catch {
      setError('El enlace no es válido o ya expiró. Solicita uno nuevo.')
    } finally {
      setSubmitting(false)
    }
  }

  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-100">
        <div className="w-full max-w-sm rounded-lg bg-white p-8 text-center shadow-md">
          <p className="mb-4 text-sm text-gray-700">Este enlace de recuperación no es válido.</p>
          <Link to="/forgot-password" className="text-sm text-blue-700 hover:underline">
            Solicitar un enlace nuevo
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-lg bg-white p-8 shadow-md">
        <h1 className="mb-6 text-xl font-semibold text-gray-900">Restablecer contraseña</h1>

        <Label htmlFor="new-password">Nueva contraseña</Label>
        <Input
          id="new-password"
          type="password"
          required
          minLength={6}
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          className="mb-4 w-full"
        />

        <Label htmlFor="confirm-password">Confirmar contraseña</Label>
        <Input
          id="confirm-password"
          type="password"
          required
          minLength={6}
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          className="mb-4 w-full"
        />

        {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

        <Button type="submit" variant="primary" disabled={submitting} className="w-full">
          {submitting ? 'Guardando...' : 'Restablecer contraseña'}
        </Button>
      </form>
    </div>
  )
}
```

- [ ] **Step 2: Add the public route**

Edit `frontend/src/App.tsx` — add the import and route:

```tsx
import { ResetPasswordPage } from './routes/ResetPasswordPage'
```

```tsx
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
```

- [ ] **Step 3: Type-check**

Run (from `frontend/`): `npx tsc -b --noEmit`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/routes/ResetPasswordPage.tsx frontend/src/App.tsx
git commit -m "feat(frontend): agrega pantalla de restablecer contraseña"
```

---

### Task 17: User maintenance screen (`/usuarios`)

Note: the spec describes the route as `/backoffice/usuarios`, but every existing backoffice route in this app is flat (`/carreras`, `/resultados`, `/notificaciones`, `/enlaces` — see `frontend/src/App.tsx`). This plan uses `/usuarios` to match that existing convention.

**Files:**
- Create: `frontend/src/features/users/UsersPage.tsx`
- Create: `frontend/src/features/users/UserFormModal.tsx`
- Modify: `frontend/src/components/AppLayout.tsx`
- Modify: `frontend/src/App.tsx`

- [ ] **Step 1: Create the user creation modal**

Create `frontend/src/features/users/UserFormModal.tsx`:

```tsx
import { useState, type FormEvent } from 'react'
import { createUser } from '../../api/endpoints'
import type { UserRole } from '../../api/types'
import { Modal, Button, Label, Input, Select } from '@nicarunner/ui'

const ROLE_OPTIONS: UserRole[] = ['Administrador', 'Capturista', 'Lector']

interface UserFormModalProps {
  onClose: () => void
  onSaved: () => void
}

export function UserFormModal({ onClose, onSaved }: UserFormModalProps) {
  const [email, setEmail] = useState('')
  const [nombre, setNombre] = useState('')
  const [role, setRole] = useState<UserRole>('Lector')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await createUser({ email, nombre, role })
      onSaved()
    } catch {
      setError('No se pudo crear el usuario. Verifica que el email no esté ya registrado.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal onClose={onClose} labelledBy="user-form-title">
      <form onSubmit={handleSubmit}>
        <h2 id="user-form-title" className="mb-4 text-base font-semibold text-zinc-900">
          Nuevo usuario
        </h2>

        <Label htmlFor="user-email">Email</Label>
        <Input
          id="user-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="mb-3 w-full"
        />

        <Label htmlFor="user-nombre">Nombre</Label>
        <Input
          id="user-nombre"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          className="mb-3 w-full"
        />

        <Label htmlFor="user-role">Rol</Label>
        <Select
          id="user-role"
          value={role}
          onChange={(e) => setRole(e.target.value as UserRole)}
          className="mb-3 w-full"
        >
          {ROLE_OPTIONS.map((r) => (
            <option key={r} value={r}>
              {r}
            </option>
          ))}
        </Select>

        {error && <p className="mb-3 text-sm text-critical-600">{error}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? 'Guardando...' : 'Crear'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
```

- [ ] **Step 2: Create the users page**

Create `frontend/src/features/users/UsersPage.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { getUsers, updateUser } from '../../api/endpoints'
import type { UserDto, UserRole } from '../../api/types'
import { useAuth } from '../../auth/auth-context'
import { Button, DataTable, LoadingText, EmptyState, Select } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'
import { UserFormModal } from './UserFormModal'

const ROLE_OPTIONS: UserRole[] = ['Administrador', 'Capturista', 'Lector']

export function UsersPage() {
  const { user: currentUser } = useAuth()
  const [users, setUsers] = useState<UserDto[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)

  function reload() {
    setLoading(true)
    getUsers()
      .then(setUsers)
      .finally(() => setLoading(false))
  }

  // Effect-driven fetch with a loading flag: react.dev/learn/synchronizing-with-effects#fetching-data
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(reload, [])

  async function handleRoleChange(target: UserDto, role: UserRole) {
    await updateUser(target.id, { role })
    reload()
  }

  async function handleToggleActive(target: UserDto) {
    await updateUser(target.id, { isActive: !target.isActive })
    reload()
  }

  const columns: Column<UserDto>[] = [
    { header: 'Email', render: (u) => u.email },
    { header: 'Nombre', render: (u) => u.nombre },
    {
      header: 'Rol',
      render: (u) => {
        const isSelf = u.id === currentUser?.userId
        return (
          <Select
            value={u.role}
            disabled={isSelf}
            onChange={(e) => handleRoleChange(u, e.target.value as UserRole)}
          >
            {ROLE_OPTIONS.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </Select>
        )
      },
    },
    {
      header: 'Estado',
      render: (u) => (u.isActive ? 'Activo' : 'Inactivo'),
    },
    {
      header: 'Creado',
      render: (u) => new Date(u.createdAt).toLocaleDateString(),
    },
    {
      header: '',
      render: (u) => {
        const isSelf = u.id === currentUser?.userId
        return (
          <Button
            size="sm"
            variant={u.isActive ? 'destructive' : 'primary'}
            disabled={isSelf}
            onClick={() => handleToggleActive(u)}
          >
            {u.isActive ? 'Desactivar' : 'Activar'}
          </Button>
        )
      },
    },
  ]

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-end">
        <Button variant="primary" onClick={() => setShowCreate(true)}>
          Nuevo usuario
        </Button>
      </div>

      {loading && <LoadingText message="Cargando usuarios..." />}

      {!loading && (
        <DataTable
          columns={columns}
          data={users}
          rowKey={(u) => u.id}
          emptyState={<EmptyState message="Todavía no hay usuarios de backoffice." />}
        />
      )}

      {showCreate && (
        <UserFormModal
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false)
            reload()
          }}
        />
      )}
    </div>
  )
}
```

- [ ] **Step 3: Add the nav entry (Administrador-only)**

Edit `frontend/src/components/AppLayout.tsx` — replace the `NAV_ITEMS` constant and the `nav` rendering:

```tsx
const NAV_ITEMS = [
  { to: '/', label: 'Dashboard' },
  { to: '/carreras', label: 'Carreras' },
  { to: '/resultados', label: 'Resultados' },
  { to: '/notificaciones', label: 'Notificaciones' },
  { to: '/enlaces', label: 'Enlaces públicos' },
] as const

const ADMIN_ONLY_NAV_ITEMS = [{ to: '/usuarios', label: 'Usuarios' }] as const
```

```tsx
            <nav className="flex items-center gap-1">
              {NAV_ITEMS.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    `px-3 py-1.5 text-sm font-medium ${
                      isActive ? 'bg-slate-blue-800 text-white' : 'text-zinc-400 hover:text-white'
                    }`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
              {user?.role === 'Administrador' &&
                ADMIN_ONLY_NAV_ITEMS.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    className={({ isActive }) =>
                      `px-3 py-1.5 text-sm font-medium ${
                        isActive ? 'bg-slate-blue-800 text-white' : 'text-zinc-400 hover:text-white'
                      }`
                    }
                  >
                    {item.label}
                  </NavLink>
                ))}
            </nav>
```

- [ ] **Step 4: Wire the route (Administrador-only)**

Edit `frontend/src/App.tsx` — add the import and the route inside the existing `AppLayout` route group:

```tsx
import { UsersPage } from './features/users/UsersPage'
```

```tsx
              <Route element={<AppLayout />}>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/carreras" element={<RacesPage />} />
                <Route path="/carreras/:raceId" element={<RaceDetailPage />} />
                <Route path="/resultados" element={<ResultsPage />} />
                <Route path="/notificaciones" element={<NotificationsPage />} />
                <Route path="/enlaces" element={<PublicLinksPage />} />

                <Route element={<ProtectedRoute allowedRoles={['Administrador']} />}>
                  <Route path="/usuarios" element={<UsersPage />} />
                </Route>
              </Route>
```

(Nesting a second `<ProtectedRoute allowedRoles={['Administrador']} />` inside the outer `Administrador`/`Lector`-gated tree narrows just this one route to admins — a `Lector` hitting `/usuarios` sees the existing "tu rol no tiene acceso" message from `ProtectedRoute` instead of the page.)

- [ ] **Step 5: Type-check**

Run (from `frontend/`): `npx tsc -b --noEmit`
Expected: no errors.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/users/ frontend/src/components/AppLayout.tsx frontend/src/App.tsx
git commit -m "feat(frontend): agrega pantalla de mantenimiento de usuarios de backoffice"
```

---

### Task 18: End-to-end manual verification

This repo has no frontend test runner, so the login redesign, forced password change, forgot/reset flow, and user maintenance screen need a real browser pass.

- [ ] **Step 1: Run the full backend test suite one more time**

Run: `dotnet test ../../tests/NicaRunner.Tests/NicaRunner.Tests.csproj` (from `src/NicaRunner.Api`)
Expected: all tests pass.

- [ ] **Step 2: Configure a local seed password and start the backend**

Run (from `src/NicaRunner.Api`):
```bash
dotnet user-secrets set "Seed:DefaultAdminPassword" "TempoLocal123!"
dotnet run
```
Expected: server starts on the dev Sqlite DB; console shows no seed-related errors.

- [ ] **Step 3: Start the frontend and open it in the browser preview**

From `frontend/`, start the dev server (`npm run dev`) using the project's preview tooling, then navigate to the login page.

- [ ] **Step 4: Visually verify the login redesign**

Confirm: the NicaRunner logo renders top-left of a dark brand panel on desktop width (≥1024px), the form panel is on the right, "¿Olvidaste tu contraseña?" link is visible under the password field. Resize to mobile width (375px) and confirm the brand panel disappears and the logo appears small, centered above the form instead.

- [ ] **Step 5: Verify the forced change-password flow**

Log in as `hilbert.mendez@gmail.com` with `TempoLocal123!`. Confirm you're redirected to `/change-password` and cannot navigate to `/`, `/carreras`, etc. (URL bar navigation should also bounce back). Submit a new password; confirm you land on the dashboard afterward. Log out, log back in with the new password, confirm you go straight to the dashboard (no forced redirect this time).

- [ ] **Step 6: Verify forgot/reset password**

From the login page, click "¿Olvidaste tu contraseña?", submit `evr86.skip@gmail.com`. Confirm the generic confirmation message appears regardless. Check the backend console/logs for the Resend call (it will fail without a real `Resend:ApiKey` configured locally — confirm the failure is logged as a notification-send failure, not an unhandled exception, and that `POST /api/auth/forgot-password` still returned `200 OK` to the browser).

- [ ] **Step 7: Verify the user maintenance screen**

Log in as an admin whose `mustChangePassword` is already `false` (e.g. after Step 5). Confirm the "Usuarios" nav item appears (only for `Administrador`). Open `/usuarios`, confirm the 3 seeded admins are listed. Create a new user with role `Lector`. Confirm it appears in the list. Try changing your own role in the table — confirm the role `<Select>` is disabled on your own row, and the "Desactivar" button is disabled on your own row too.

- [ ] **Step 8: Verify a non-admin is blocked from `/usuarios`**

Log in as the `Lector` user created in Step 7. Confirm the "Usuarios" nav item is absent, and navigating directly to `/usuarios` in the URL bar shows the existing "tu rol no tiene acceso" message instead of the page.

- [ ] **Step 9: Stop both dev servers** once verification is complete.

---

## Summary of new configuration required in Render (production) before this ships

- `Seed__DefaultAdminPassword` — temporary password shared with the 3 seeded admins out-of-band.
- `Frontend__BaseUrl` — the production frontend URL (used to build the password-reset link).

Both follow the same double-underscore env var convention already used for `Resend__ApiKey` in this project.
