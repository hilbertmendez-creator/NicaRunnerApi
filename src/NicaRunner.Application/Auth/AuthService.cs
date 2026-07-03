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

        if (!user.IsActive)
            throw new ForbiddenException("Esta cuenta está desactivada.");

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

        var emailSender = notificationSenders.FirstOrDefault(s => s.Channel == NotificationChannel.Email);
        if (emailSender is null)
            return;

        var resetLink = $"{frontendOptions.Value.BaseUrl}/reset-password?token={user.PasswordResetToken}";
        var mensaje = $"Hola {user.Nombre}, recibimos una solicitud para restablecer tu contraseña de NicaRunner Backoffice. " +
            $"Este link es válido por 30 minutos: {resetLink}\n\nSi no solicitaste esto, ignora este correo.";

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
