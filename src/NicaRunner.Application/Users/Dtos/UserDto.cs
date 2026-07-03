using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

public record UserDto(
    int Id,
    string Email,
    string Nombre,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);
