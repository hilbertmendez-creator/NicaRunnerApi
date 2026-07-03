using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Users.Dtos;

public record UpdateUserRequest(UserRole? Role, bool? IsActive);
