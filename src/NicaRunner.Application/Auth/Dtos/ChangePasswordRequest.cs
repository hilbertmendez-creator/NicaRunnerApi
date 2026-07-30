using System.ComponentModel.DataAnnotations;
using NicaRunner.Application.Common.Validation;

namespace NicaRunner.Application.Auth.Dtos;

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StrongPassword] string NewPassword);
