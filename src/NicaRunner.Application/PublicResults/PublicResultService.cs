using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.PublicResults.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.PublicResults;

public class PublicResultService(
    IPublicResultTokenRepository tokenRepository,
    IRaceRepository raceRepository,
    IRaceCategoryRepository categoryRepository,
    IRunnerRepository runnerRepository,
    IResultRepository resultRepository) : IPublicResultService
{
    public async Task<PublicTokenDto> CreateTokenAsync(int raceId, CreatePublicTokenRequest request, int creatorId, CancellationToken ct = default)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");

        var token = new PublicResultToken
        {
            RaceId = raceId,
            Token = GenerateToken(),
            FechaExpiracion = DateTime.UtcNow.AddDays(request.DiasExpiracion),
            CreatedBy = creatorId
        };

        await tokenRepository.AddAsync(token, ct);
        await tokenRepository.SaveChangesAsync(ct);

        return ToDto(token);
    }

    public async Task<List<PublicTokenDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");

        var tokens = await tokenRepository.GetAllByRaceAsync(raceId, ct);
        return tokens.Select(ToDto).ToList();
    }

    public async Task<PublicRaceResultsDto> GetResultsByTokenAsync(string token, CancellationToken ct = default)
    {
        var (race, _) = await ResolveValidTokenAsync(token, ct);

        var categories = await categoryRepository.GetAllByRaceAsync(race.Id, ct);
        var runnersById = (await runnerRepository.GetAllByRaceAsync(race.Id, ct)).ToDictionary(r => r.Id);
        var results = await resultRepository.GetAllByRaceAsync(race.Id, ct);

        var categorias = categories
            .Select(category => new PublicCategoryResultsDto(
                category.NombreCategoria,
                category.Distancia,
                results
                    .Where(r => r.CategoryId == category.Id)
                    .OrderBy(r => r.Posicion)
                    .Select(r => new PublicRunnerResultDto(
                        r.RunnerId,
                        runnersById.TryGetValue(r.RunnerId, out var runner) ? runner.Nombre : "(desconocido)",
                        r.Dorsal,
                        r.Posicion,
                        r.TiempoLlegada))
                    .ToList()))
            .ToList();

        return new PublicRaceResultsDto(race.Nombre, race.FechaCarrera, categorias);
    }

    public async Task<PublicRunnerDetailDto> GetRunnerResultByTokenAsync(string token, int runnerId, CancellationToken ct = default)
    {
        var (race, _) = await ResolveValidTokenAsync(token, ct);

        var results = await resultRepository.GetAllByRaceAsync(race.Id, ct);
        var result = results.FirstOrDefault(r => r.RunnerId == runnerId)
            ?? throw new NotFoundException($"No hay resultado registrado para el corredor {runnerId} en esta carrera.");

        var category = await categoryRepository.GetByIdAsync(race.Id, result.CategoryId, ct)
            ?? throw new NotFoundException("No se encontró la categoría del resultado.");

        var runner = await runnerRepository.GetByIdAsync(race.Id, runnerId, ct)
            ?? throw new NotFoundException($"No existe el corredor con id {runnerId} en esta carrera.");

        return new PublicRunnerDetailDto(
            race.Nombre,
            category.NombreCategoria,
            category.Distancia,
            runner.Id,
            runner.Nombre,
            result.Dorsal,
            result.Posicion,
            result.TiempoLlegada);
    }

    private async Task<(Race Race, PublicResultToken Token)> ResolveValidTokenAsync(string token, CancellationToken ct)
    {
        var tokenEntity = await tokenRepository.GetByTokenAsync(token, ct)
            ?? throw new NotFoundException("El enlace público no es válido.");

        if (tokenEntity.IsExpired || tokenEntity.FechaExpiracion < DateTime.UtcNow)
            throw new NotFoundException("El enlace público ha expirado.");

        var race = await raceRepository.GetByIdAsync(tokenEntity.RaceId, ct)
            ?? throw new NotFoundException("La carrera asociada a este enlace ya no existe.");

        return (race, tokenEntity);
    }

    private static string GenerateToken() =>
        $"{Guid.NewGuid():N}{Guid.NewGuid():N}";

    private static PublicTokenDto ToDto(PublicResultToken token) => new(
        token.Id,
        token.RaceId,
        token.Token,
        token.FechaExpiracion,
        token.CreatedAt);
}
