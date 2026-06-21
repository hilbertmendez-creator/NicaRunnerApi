using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string Nombre,
    [Required] UserRole Role);
