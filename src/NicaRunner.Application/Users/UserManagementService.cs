using System.Security.Cryptography;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications.EmailTemplates;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users;

public class UserManagementService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEnumerable<INotificationSender> notificationSenders,
    IEmailTemplateRenderer emailTemplateRenderer,
    IAuditService auditService,
    AliasAssigner aliasAssigner) : IUserManagementService
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

        // Asigna alias + agrega + persiste con reintento ante colisión TOCTOU
        // (único camino interactivo de creación — design.md §2.2/§3.4).
        await aliasAssigner.AddWithUniqueAliasAsync(user, ct);

        var emailSender = notificationSenders.FirstOrDefault(s => s.Channel == NotificationChannel.Email);
        if (emailSender is not null)
        {
            var rendered = emailTemplateRenderer.RenderWelcomeAccount(new WelcomeAccountEmailModel(user.Nombre, tempPassword));
            await emailSender.SendAsync(user.Email, rendered.Text, rendered.Subject, rendered.Html, ct);
        }

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

        if (ProtectedSeedUsers.IsProtected(user.Email))
        {
            if (request.IsActive is false)
                throw new ForbiddenException("No se puede desactivar un usuario administrador semilla.");
            if (request.Role is not null && request.Role != user.Role)
                throw new ForbiddenException("No se puede cambiar el rol de un usuario administrador semilla.");
        }

        // Diff de valores viejos (aún en memoria, sin query extra) antes de mutar.
        var changes = new List<FieldChange>();

        if (request.Nombre is { } nombreRaw)
        {
            var nombre = nombreRaw.Trim();
            if (nombre.Length == 0)
                throw new ValidationException("El nombre no puede estar vacío.");
            changes.Add(new FieldChange("Nombre", user.Nombre, nombre));
            user.Nombre = nombre;
        }
        if (request.Role is { } role)
        {
            changes.Add(new FieldChange("Role", AuditValue.Of(user.Role), AuditValue.Of(role)));
            user.Role = role;
        }
        if (request.IsActive is { } isActive)
        {
            changes.Add(new FieldChange("IsActive", AuditValue.Of(user.IsActive), AuditValue.Of(isActive)));
            user.IsActive = isActive;
        }
        if (request.Username is { } usernameRaw)
        {
            var username = usernameRaw.Trim().ToLowerInvariant();
            if (!AliasGenerator.IsValidAliasFormat(username))
                throw new ValidationException("El alias no tiene un formato válido.");

            // No-op si el admin reenvía el alias actual: ni 409 ni entrada de auditoría
            // (user-management: "Admin edits alias to a value already taken" solo aplica
            // cuando el alias pertenece a OTRO usuario).
            if (username != user.Username)
            {
                if (await userRepository.UsernameExistsAsync(username, ct))
                    throw new ConflictException($"El alias '{username}' ya está en uso.");

                changes.Add(new FieldChange("Username", user.Username, username));
                user.Username = username;
            }
        }

        // Encola solo lo que cambió; se persiste junto al usuario en el mismo SaveChanges.
        auditService.TrackChanges(AuditEntityTypes.User, user.Id, currentUserId, changes);

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
        user.CreatedAt,
        user.Username);
}
