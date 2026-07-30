using System.ComponentModel.DataAnnotations;
using NicaRunner.Application.Common.Validation;

namespace NicaRunner.Application.Auth.Dtos;

public record ResetPasswordRequest(
    [Required] string Token,
    [Required, StrongPassword] string NewPassword);
