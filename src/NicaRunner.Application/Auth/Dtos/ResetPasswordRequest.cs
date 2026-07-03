using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Auth.Dtos;

public record ResetPasswordRequest(
    [Required] string Token,
    [Required, MinLength(6)] string NewPassword);
