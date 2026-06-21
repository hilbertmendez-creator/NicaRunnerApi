using NicaRunner.Application.Auth.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IAuthService
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
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException("Email o contraseña incorrectos.");

        return BuildAuthResponse(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var generated = jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(generated.Token, generated.ExpiresAtUtc, user.Id, user.Email, user.Nombre, user.Role);
    }
}
