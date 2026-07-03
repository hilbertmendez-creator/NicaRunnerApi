using System.Security.Cryptography;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users;

public class UserManagementService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    INotificationDispatcher notificationDispatcher) : IUserManagementService
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
        // Si el canal Email no está configurado el dispatcher devuelve failure
        // silencioso — mantenemos el comportamiento previo (no bloqueamos el
        // CreateAsync por un email que no salió; el admin puede compartir la
        // password temporal manualmente).
        await notificationDispatcher.SendAsync(
            NotificationChannel.Email, user.Email, mensaje, "Tu cuenta en NicaRunner Backoffice", ct);

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
