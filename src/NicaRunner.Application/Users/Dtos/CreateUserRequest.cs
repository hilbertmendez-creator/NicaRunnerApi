using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

// Nombre acotado a 120 para igualar el límite ya existente en UpdateUserRequest
// (user-management: Create Request Field Length Consistency). Sin campo de alias:
// la creación siempre lo genera (design.md §4.2).
public record CreateUserRequest(
    [Required, EmailAddress] string Email,
    [Required, MaxLength(120)] string Nombre,
    [Required] UserRole Role);
