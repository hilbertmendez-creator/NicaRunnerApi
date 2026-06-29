using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IGoogleAuthService googleAuthService,
    IRefreshTokenService refreshTokenService) : IAuthService
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

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive || user.PasswordHash is null ||
            !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException("Email o contraseña incorrectos.");

        return await BuildAuthResponseAsync(user, ct);
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

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
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
            result.User.Role);
    }

    public Task LogoutAsync(LogoutRequest request, CancellationToken ct = default) =>
        refreshTokenService.LogoutAsync(request.RefreshToken, ct);

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
            user.Role);
    }
}
