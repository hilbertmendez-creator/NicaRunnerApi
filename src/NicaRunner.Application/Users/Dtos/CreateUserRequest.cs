using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

public record CreateUserRequest(
    [Required, EmailAddress] string Email,
    [Required] string Nombre,
    [Required] UserRole Role);
