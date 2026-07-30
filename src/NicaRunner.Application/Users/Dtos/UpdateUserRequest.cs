using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

// Parche parcial: cada campo es opcional. Nombre y Username van al final con default
// para no romper llamadas posicionales existentes (UpdateUserRequest(role, isActive)).
// Username usa el namespace amplio (admin-editable, "user-management: Admin-Editable
// Alias"); AliasGenerator.IsValidAliasFormat re-valida server-side en UpdateAsync.
public record UpdateUserRequest(
    UserRole? Role,
    bool? IsActive,
    [MaxLength(120)] string? Nombre = null,
    [RegularExpression("^[a-z0-9._-]{3,30}$")] string? Username = null);
