using NicaRunner.Application.Common.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IRaceRepository
{
    Task<Race?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Race?> GetByJoinCodeAsync(string joinCode, CancellationToken ct = default);
    Task<List<Race>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// stale-race-sweep: todas las carreras en un Estado dado, sin paginar — el barrido
    /// anti-zombie corre sobre TODAS las EnCurso, no sobre una página del backoffice.
    /// </summary>
    Task<List<Race>> GetByStatusAsync(RaceStatus status, CancellationToken ct = default);
    Task<PaginatedList<Race>> GetPaginatedAsync(int limit = 50, int offset = 0, CancellationToken ct = default);
    Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken ct = default);
    Task AddAsync(Race race, CancellationToken ct = default);
    Task AddJudgeAsync(RaceJudge judge, CancellationToken ct = default);
    Task<bool> IsJudgeAsync(int raceId, int userId, CancellationToken ct = default);
    void Remove(Race race);
    Task SaveChangesAsync(CancellationToken ct = default);
}
