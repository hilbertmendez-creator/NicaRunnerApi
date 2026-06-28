using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Races;

public class RaceService(IRaceRepository raceRepository) : IRaceService
{
    // Sin I, O, 0, 1 para evitar confusiones visuales al teclear el código en el móvil.
    private const string JoinCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int JoinCodeLength = 6;
    private const int MaxJoinCodeAttempts = 10;

    public async Task<RaceDto> CreateAsync(CreateRaceRequest request, int adminId, CancellationToken ct = default)
    {
        var race = new Race
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            FechaCarrera = request.FechaCarrera,
            AdminId = adminId,
            JoinCode = await GenerateUniqueJoinCodeAsync(ct)
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

    public async Task<RaceDto> StartAsync(int raceId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);

        if (race.Estado != RaceStatus.Planeada)
            throw new ConflictException($"Solo se puede iniciar una carrera en estado Planeada (estado actual: {race.Estado}).");

        race.Estado = RaceStatus.EnCurso;
        race.RaceStartUtc = DateTime.UtcNow;
        race.UpdatedAt = DateTime.UtcNow;

        await raceRepository.SaveChangesAsync(ct);
        return ToDto(race);
    }

    public async Task<RaceDto> JoinByCodeAsync(JoinByCodeRequest request, int userId, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var race = await raceRepository.GetByJoinCodeAsync(code, ct)
            ?? throw new NotFoundException("No existe una carrera con ese código.");

        if (race.Estado == RaceStatus.Terminada)
            throw new ConflictException("La carrera ya está terminada y no acepta nuevos jueces.");

        if (race.AdminId == userId)
            return ToDto(race);

        if (!await raceRepository.IsJudgeAsync(race.Id, userId, ct))
        {
            await raceRepository.AddJudgeAsync(new RaceJudge { RaceId = race.Id, UserId = userId }, ct);
            await raceRepository.SaveChangesAsync(ct);
        }

        return ToDto(race);
    }

    private async Task<Race> GetRaceOrThrowAsync(int raceId, CancellationToken ct) =>
        await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxJoinCodeAttempts; attempt++)
        {
            var code = GenerateJoinCode();
            if (!await raceRepository.JoinCodeExistsAsync(code, ct))
                return code;
        }
        throw new InvalidOperationException("No se pudo generar un JoinCode único tras varios intentos.");
    }

    private static string GenerateJoinCode()
    {
        var chars = new char[JoinCodeLength];
        for (var i = 0; i < JoinCodeLength; i++)
            chars[i] = JoinCodeAlphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(JoinCodeAlphabet.Length)];
        return new string(chars);
    }

    private static RaceDto ToDto(Race race) => new(
        race.Id,
        race.Nombre,
        race.Descripcion,
        race.FechaCarrera,
        race.Estado,
        race.JoinCode,
        race.RaceStartUtc,
        race.AdminId,
        race.CreatedAt,
        race.UpdatedAt);
}
