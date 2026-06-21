using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Auth.Dtos;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
