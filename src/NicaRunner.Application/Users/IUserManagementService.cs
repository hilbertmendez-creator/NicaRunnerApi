using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Users.Dtos;

namespace NicaRunner.Application.Users;

public interface IUserManagementService
{
    Task<PaginatedList<UserDto>> GetAllAsync(int limit = 50, int offset = 0, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(int currentUserId, int targetUserId, UpdateUserRequest request, CancellationToken ct = default);

    // login-lockout: "Admin Unlock" — limpia FailedLoginCount/LockedUntilUtc y audita.
    Task<UserDto> UnlockAsync(int currentUserId, int targetUserId, CancellationToken ct = default);
}
