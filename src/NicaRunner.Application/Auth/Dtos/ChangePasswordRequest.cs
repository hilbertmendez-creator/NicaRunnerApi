using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Auth.Dtos;

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(6)] string NewPassword);
