using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth.Dtos;

public record CurrentUserDto(
    int UserId,
    string Email,
    string Nombre,
    UserRole Role,
    bool MustChangePassword);
