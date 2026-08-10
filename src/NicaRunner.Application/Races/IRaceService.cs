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

    /// <summary>
    /// Cierra la carrera cascadeando sobre sus RaceCategory EnCurso — nunca escribe
    /// Race.Estado directamente (ver RaceStatusDeriver). 409 si hay disputas
    /// abiertas, capturas sin dorsal (no Anuladas), o categorías todavía Planeada.
    /// Idempotente: una carrera ya Terminada devuelve 200 sin mutar nada.
    /// </summary>
    Task<RaceDto> CloseAsync(int raceId, int actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Reabre la carrera cascadeando sobre sus RaceCategory Terminada — mismo
    /// principio que CloseAsync, nunca escribe Race.Estado directamente.
    /// Idempotente: una carrera ya EnCurso/Planeada devuelve 200 sin mutar nada.
    /// </summary>
    Task<RaceDto> ReopenAsync(int raceId, int actorUserId, CancellationToken ct = default);
}
