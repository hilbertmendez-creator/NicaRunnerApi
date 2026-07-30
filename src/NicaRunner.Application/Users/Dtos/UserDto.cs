using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

// Username es nullable hasta que M4 (PR2) lo vuelva NOT NULL en la entidad; en la
// práctica, tras el backfill (M2) y con los 3 sitios de creación conectados, todo
// usuario nuevo ya trae alias.
public record UserDto(
    int Id,
    string Email,
    string Nombre,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    string? Username);
