using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.ReservedDorsals.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.ReservedDorsals;

public class ReservedDorsalService(
    IReservedDorsalRepository reservedDorsalRepository,
    IRaceRepository raceRepository) : IReservedDorsalService
{
    public async Task<List<ReservedDorsalDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var reserved = await reservedDorsalRepository.GetAllByRaceAsync(raceId, ct);
        return reserved.Select(ToDto).ToList();
    }

    public async Task<ReservedDorsalDto> CreateAsync(int raceId, CreateReservedDorsalRequest request, int adminId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        if (await reservedDorsalRepository.IsReservedAsync(raceId, request.Dorsal, ct))
            throw new ConflictException($"El dorsal '{request.Dorsal}' ya está reservado en esta carrera.");

        var reservedDorsal = new ReservedDorsal
        {
            RaceId = raceId,
            Dorsal = request.Dorsal.Trim(),
            DorsalNormalizado = DorsalNormalizer.Normalize(request.Dorsal),
            Motivo = request.Motivo,
            CreatedBy = adminId
        };

        await reservedDorsalRepository.AddAsync(reservedDorsal, ct);
        await reservedDorsalRepository.SaveChangesAsync(ct);

        return ToDto(reservedDorsal);
    }

    public async Task DeleteAsync(int raceId, int reservedDorsalId, CancellationToken ct = default)
    {
        var reservedDorsal = await reservedDorsalRepository.GetByIdAsync(raceId, reservedDorsalId, ct)
            ?? throw new NotFoundException($"No existe la reserva de dorsal con id {reservedDorsalId} en esta carrera.");

        reservedDorsalRepository.Remove(reservedDorsal);
        await reservedDorsalRepository.SaveChangesAsync(ct);
    }

    private async Task EnsureRaceExistsAsync(int raceId, CancellationToken ct)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");
    }

    private static ReservedDorsalDto ToDto(ReservedDorsal reservedDorsal) => new(
        reservedDorsal.Id,
        reservedDorsal.RaceId,
        reservedDorsal.Dorsal,
        reservedDorsal.Motivo,
        reservedDorsal.CreatedBy,
        reservedDorsal.CreatedAt);
}
