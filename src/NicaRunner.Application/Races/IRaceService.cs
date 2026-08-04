using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Races.Dtos;

namespace NicaRunner.Application.Races;

public interface IRaceService
{
    Task<RaceDto> CreateAsync(CreateRaceRequest request, int adminId, CancellationToken ct = default);
    Task<PaginatedList<RaceDto>> GetAllAsync(int limit = 50, int offset = 0, CancellationToken ct = default);
    Task<RaceDto> GetByIdAsync(int raceId, CancellationToken ct = default);
    Task<RaceDto> UpdateAsync(int raceId, UpdateRaceRequest request, int currentUserId, CancellationToken ct = default);
    Task DeleteAsync(int raceId, CancellationToken ct = default);
    Task<RaceDto> StartAsync(int raceId, CancellationToken ct = default);
    Task<RaceDto> JoinByCodeAsync(JoinByCodeRequest request, int userId, CancellationToken ct = default);
}
