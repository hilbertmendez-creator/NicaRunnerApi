using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

// Parche parcial: cada campo es opcional. Nombre va al final con default para no
// romper llamadas posicionales existentes (UpdateUserRequest(role, isActive)).
public record UpdateUserRequest(
    UserRole? Role,
    bool? IsActive,
    [MaxLength(120)] string? Nombre = null);
