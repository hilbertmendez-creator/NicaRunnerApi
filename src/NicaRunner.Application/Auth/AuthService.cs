using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IGoogleAuthService googleAuthService) : IAuthService
{
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

    private AuthResponse BuildAuthResponse(User user)
    {
        var generated = jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(generated.Token, generated.ExpiresAtUtc, user.Id, user.Email, user.Nombre, user.Role, user.MustChangePassword);
    }
}
