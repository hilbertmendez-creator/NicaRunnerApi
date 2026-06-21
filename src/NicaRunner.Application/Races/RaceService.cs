using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Races;

public class RaceService(IRaceRepository raceRepository) : IRaceService
{
    public async Task<RaceDto> CreateAsync(CreateRaceRequest request, int adminId, CancellationToken ct = default)
    {
        var race = new Race
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            FechaCarrera = request.FechaCarrera,
            AdminId = adminId
        };

        await raceRepository.AddAsync(race, ct);
        await raceRepository.SaveChangesAsync(ct);

        return ToDto(race);
    }

    public async Task<List<RaceDto>> GetAllAsync(CancellationToken ct = default)
    {
        var races = await raceRepository.GetAllAsync(ct);
        return races.Select(ToDto).ToList();
    }

    public async Task<RaceDto> GetByIdAsync(int raceId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        return ToDto(race);
    }

    public async Task<RaceDto> UpdateAsync(int raceId, UpdateRaceRequest request, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);

        race.Nombre = request.Nombre;
        race.Descripcion = request.Descripcion;
        race.FechaCarrera = request.FechaCarrera;
        race.Estado = request.Estado;
        race.UpdatedAt = DateTime.UtcNow;

        await raceRepository.SaveChangesAsync(ct);
        return ToDto(race);
    }

    public async Task DeleteAsync(int raceId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        raceRepository.Remove(race);
        await raceRepository.SaveChangesAsync(ct);
    }

    private async Task<Race> GetRaceOrThrowAsync(int raceId, CancellationToken ct) =>
        await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

    private static RaceDto ToDto(Race race) => new(
        race.Id,
        race.Nombre,
        race.Descripcion,
        race.FechaCarrera,
        race.Estado,
        race.AdminId,
        race.CreatedAt,
        race.UpdatedAt);
}
