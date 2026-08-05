using NicaRunner.Application.ReservedDorsals.Dtos;

namespace NicaRunner.Application.ReservedDorsals;

public interface IReservedDorsalService
{
    Task<List<ReservedDorsalDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<ReservedDorsalDto> CreateAsync(int raceId, CreateReservedDorsalRequest request, int adminId, CancellationToken ct = default);

    /// <summary>
    /// design.md D8/D9 File Changes: DELETE es obligatorio, no opcional — sin un
    /// endpoint de liberación, un Dorsal reservado quedaría permanentemente inutilizable
    /// (la reserva bloquea también el camino manual de RunnerService).
    /// </summary>
    Task DeleteAsync(int raceId, int reservedDorsalId, CancellationToken ct = default);
}
