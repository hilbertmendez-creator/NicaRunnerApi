using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications.EmailTemplates;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IGoogleAuthService googleAuthService,
    IRefreshTokenService refreshTokenService,
    IEnumerable<INotificationSender> notificationSenders,
    IEmailTemplateRenderer emailTemplateRenderer,
    IOptions<FrontendOptions> frontendOptions,
    AliasAssigner aliasAssigner,
    IOptions<LockoutOptions> lockoutOptions) : IAuthService
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    // user-auth: "Generic Authentication Failure Response" — un único mensaje para
    // password incorrecta, identificador inexistente y cuenta bloqueada; ninguna
    // variante del texto puede revelar cuál de los tres casos ocurrió.
    private const string GenericLoginFailureMessage = "Credenciales inválidas.";

    // Password de relleno usada solo para computar un hash "dummy" contra el que
    // verificar cuando no hay un PasswordHash real (identificador inexistente) —
    // mantiene el mismo perfil de timing que una cuenta real (user-auth: "Unified
    // Identifier Login", último párrafo). Nunca se usa para autenticar nada.
    private const string DummyPasswordSeed = "NicaRunner-Timing-Safe-Dummy-2026";

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var identifier = request.EffectiveIdentifier.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailOrUsernameAsync(identifier, ct);

        if (user is null)
        {
            // login-lockout/user-auth: sin usuario que verificar, igual corremos un
            // Verify contra un hash dummy para no delatar por timing que el
            // identificador no existe.
            passwordHasher.Verify(request.Password, passwordHasher.Hash(DummyPasswordSeed));
            throw new InvalidCredentialsException(GenericLoginFailureMessage);
        }

        // login-lockout: "Already-locked account attempts login" — mismo 401 genérico
        // sin siquiera intentar el Verify, igual que una cuenta inactiva (abajo).
        if (IsLocked(user) || !user.IsActive || user.PasswordHash is null)
            throw new InvalidCredentialsException(GenericLoginFailureMessage);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await RegisterFailedAttemptAsync(user, ct);
            throw new InvalidCredentialsException(GenericLoginFailureMessage);
        }

        // login-lockout: "Counter resets on success".
        user.FailedLoginCount = 0;
        user.LockedUntilUtc = null;

        return await BuildAuthResponseAsync(user, ct);
    }

    private static bool IsLocked(User user) =>
        user.LockedUntilUtc is { } lockedUntil && lockedUntil > DateTime.UtcNow;

    // login-lockout: "Failed Login Attempt Tracking" + "Temporary Account Lock".
    // Threshold = 0 desactiva el lockout sin deploy (design.md §3.3); un lock ya
    // vigente no se toca acá porque IsLocked ya corta antes de llegar a este punto.
    private async Task RegisterFailedAttemptAsync(User user, CancellationToken ct)
    {
        user.FailedLoginCount++;
        user.LastFailedLoginUtc = DateTime.UtcNow;

        var threshold = lockoutOptions.Value.Threshold;
        if (threshold > 0 && user.FailedLoginCount >= threshold)
            user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(lockoutOptions.Value.DurationMinutes);

        await userRepository.SaveChangesAsync(ct);
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
                // Primer sign-in de Google también crea el User inline — mismo generador
                // que el alta manual (user-alias: "Alias Assigned on Every User-Creation
                // Path", design.md §3.4, sitio 2). Si el nombre de perfil de Google
                // normaliza a cero unidades, AliasGenerator cae al fallback por email y
                // nunca lanza, así que el sign-in no puede fallar por esto.
                await aliasAssigner.AssignAsync(user, ct);
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

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {userId}.");

        if (!user.IsActive)
            throw new ForbiddenException("Esta cuenta está desactivada.");

        if (user.PasswordHash is null || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidCredentialsException("La contraseña actual no es correcta.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;

        // Un refresh token robado no debe seguir sirviendo una vez que el
        // usuario cambia su contraseña.
        await refreshTokenService.RevokeAllForUserAsync(user.Id, ct);
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

        var emailSender = notificationSenders.FirstOrDefault(s => s.Channel == NotificationChannel.Email);
        if (emailSender is null)
            return;

        try
        {
            var resetLink = EmailLinkBuilder.BuildResetUrl(frontendOptions.Value.BaseUrl, user.PasswordResetToken);
            var rendered = emailTemplateRenderer.RenderPasswordReset(new PasswordResetEmailModel(
                user.Nombre,
                resetLink,
                (int)ResetTokenLifetime.TotalMinutes));

            await emailSender.SendAsync(user.Email, rendered.Text, rendered.Subject, rendered.Html, ct);
        }
        catch (InvalidOperationException)
        {
            // BaseUrl no configurado o URL inválida — token guardado, correo omitido.
        }
        catch (TemplateRenderException)
        {
            // Fallo interno de plantilla — no exponer al caller.
        }
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

        // Un refresh token robado no debe seguir sirviendo una vez que la
        // víctima recupera su cuenta.
        await refreshTokenService.RevokeAllForUserAsync(user.Id, ct);
        await userRepository.SaveChangesAsync(ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            throw new InvalidCredentialsException("Falta el refresh token.");

        var result = await refreshTokenService.ValidateAndRotateAsync(request.RefreshToken, ct);
        var access = jwtTokenGenerator.GenerateToken(result.User);
        return new AuthResponse(
            access.Token,
            access.ExpiresAtUtc,
            result.NewToken.Token,
            result.NewToken.ExpiresAtUtc,
            result.User.Id,
            result.User.Email,
            result.User.Nombre,
            result.User.Role,
            result.User.MustChangePassword);
    }

    public Task LogoutAsync(LogoutRequest request, CancellationToken ct = default) =>
        string.IsNullOrEmpty(request.RefreshToken)
            ? Task.CompletedTask // sin token que revocar (ya deslogueado o cookie ausente) — idempotente
            : refreshTokenService.LogoutAsync(request.RefreshToken, ct);

    public async Task<CurrentUserDto> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {userId}.");

        return new CurrentUserDto(user.Id, user.Email, user.Nombre, user.Role, user.MustChangePassword);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, CancellationToken ct)
    {
        var access = jwtTokenGenerator.GenerateToken(user);
        var refresh = await refreshTokenService.IssueAsync(user, familyId: null, ct);
        // Issue persiste el refresh via IRefreshTokenRepository.AddAsync (sin
        // SaveChanges). Lo guardamos acá para mantener una sola transacción
        // por llamada de login/register/google — si algo falla después no
        // queda un token huérfano en la BD.
        await userRepository.SaveChangesAsync(ct);
        return new AuthResponse(
            access.Token,
            access.ExpiresAtUtc,
            refresh.Token,
            refresh.ExpiresAtUtc,
            user.Id,
            user.Email,
            user.Nombre,
            user.Role,
            user.MustChangePassword);
    }
}
