using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Auth.Dtos;

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshExpiresAtUtc,
    int UserId,
    string Email,
    string Nombre,
    UserRole Role);
