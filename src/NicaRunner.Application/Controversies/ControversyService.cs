using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Controversies.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Controversies;

public class ControversyService(
    IControversyRepository controversyRepository,
    IRaceRepository raceRepository) : IControversyService
{
    public async Task<List<ControversyDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default)
    {
        await GetRaceOrThrowAsync(raceId, ct);

        var controversies = await controversyRepository.GetAllByRaceAsync(raceId, ct);
        return controversies.Select(ToDto).ToList();
    }

    public async Task<ControversySummaryDto> GetSummaryAsync(int raceId, CancellationToken ct = default)
    {
        await GetRaceOrThrowAsync(raceId, ct);

        var all = await controversyRepository.GetAllByRaceAsync(raceId, ct);
        return new ControversySummaryDto(
            all.Count(c => c.Estado == ControversyState.Abierta),
            all.Count(c => c.Estado == ControversyState.Resuelta));
    }

    public async Task<ControversyDto> ResolveAsync(int raceId, int controversyId, ResolveControversyRequest request, CancellationToken ct = default)
    {
        await GetRaceOrThrowAsync(raceId, ct);

        var estado = request.Estado?.Trim();
        if (!ControversyState.IsValid(estado))
            throw new ValidationException("El estado debe ser 'Abierta' o 'Resuelta'.");

        var controversy = await controversyRepository.GetByIdAsync(raceId, controversyId, ct)
            ?? throw new NotFoundException($"No existe la disputa con id {controversyId} en la carrera {raceId}.");

        controversy.Estado = estado!;
        controversy.ResolvedAt = controversy.Estado == ControversyState.Resuelta ? DateTime.UtcNow : null;

        await controversyRepository.SaveChangesAsync(ct);

        return ToDto(controversy);
    }

    private async Task<Race> GetRaceOrThrowAsync(int raceId, CancellationToken ct) =>
        await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

    private static ControversyDto ToDto(Controversy controversy) => new(
        controversy.Id,
        controversy.RaceId,
        controversy.Dorsal,
        controversy.Nombre,
        controversy.Categoria,
        controversy.TiempoChip,
        controversy.TiempoCaptura,
        controversy.TiempoCamara,
        controversy.Diferencia,
        controversy.Estado);
}