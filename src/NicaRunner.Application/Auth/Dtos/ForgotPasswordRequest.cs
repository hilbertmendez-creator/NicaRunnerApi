using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Auth.Dtos;

public record ForgotPasswordRequest([Required, EmailAddress] string Email);
