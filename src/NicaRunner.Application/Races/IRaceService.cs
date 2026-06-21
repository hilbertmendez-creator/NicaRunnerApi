using NicaRunner.Application.Races.Dtos;

namespace NicaRunner.Application.Races;

public interface IRaceService
{
    Task<RaceDto> CreateAsync(CreateRaceRequest request, int adminId, CancellationToken ct = default);
    Task<List<RaceDto>> GetAllAsync(CancellationToken ct = default);
    Task<RaceDto> GetByIdAsync(int raceId, CancellationToken ct = default);
    Task<RaceDto> UpdateAsync(int raceId, UpdateRaceRequest request, CancellationToken ct = default);
    Task DeleteAsync(int raceId, CancellationToken ct = default);
}
