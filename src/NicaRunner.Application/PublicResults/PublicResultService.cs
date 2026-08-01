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
                    .Where(r => r.CategoryId == category.Id && r.RunnerId is not null)
                    .OrderBy(r => r.Posicion)
                    .Select(r =>
                    {
                        runnersById.TryGetValue(r.RunnerId!.Value, out var runner);
                        return new PublicRunnerResultDto(
                            r.RunnerId.Value,
                            runner?.Nombre ?? "(desconocido)",
                            r.Dorsal ?? string.Empty,
                            r.Posicion,
                            r.TiempoLlegada,
                            runner?.PublicShareKey);
                    })
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

        var categoryId = result.CategoryId
            ?? throw new NotFoundException("Este resultado todavía no tiene un dorsal/categoría asignado.");
        var category = await categoryRepository.GetByIdAsync(race.Id, categoryId, ct)
            ?? throw new NotFoundException("No se encontró la categoría del resultado.");

        var runner = await runnerRepository.GetByIdAsync(race.Id, runnerId, ct)
            ?? throw new NotFoundException($"No existe el corredor con id {runnerId} en esta carrera.");

        return new PublicRunnerDetailDto(
            race.Nombre,
            category.NombreCategoria,
            category.Distancia,
            runner.Id,
            runner.Nombre,
            result.Dorsal ?? string.Empty,
            result.Posicion,
            result.TiempoLlegada);
    }

    // design.md Decisión 3/7: 3 consultas totales — GetByShareKeyAsync (+Race +Category),
    // GetByRunnerWithCategoryAsync (+Category), y GetPlacingCountsAsync (los 4 agregados
    // en una sola consulta) solo cuando ya existe un Result. Mensaje 404 uniforme: no
    // distingue "clave inexistente" de "corredor borrado" — ver spec.md, ningún oráculo
    // de enumeración.
    public async Task<PublicRunnerShareDto> GetRunnerByShareKeyAsync(string shareKey, CancellationToken ct = default)
    {
        var runner = await runnerRepository.GetByShareKeyAsync(shareKey, ct)
            ?? throw new NotFoundException("El enlace no es válido.");

        var race = runner.Race;
        var slogan = RaceSloganSelector.Select(race.Slogan, race.Descripcion, shareKey);

        var result = await resultRepository.GetByRunnerWithCategoryAsync(runner.RaceId, runner.Id, ct);

        if (result is null)
        {
            return new PublicRunnerShareDto(
                race.Nombre, slogan, race.FechaCarrera,
                runner.Nombre, runner.Apellidos, runner.Club, runner.Dorsal,
                runner.Category.NombreCategoria, runner.Category.Distancia,
                null, null, null, null, null, null);
        }

        // design.md Decisión 7: si Result.CategoryId difiere del Runner.CategoryId
        // registrado, la categoría MOSTRADA y la usada para el placing son la del
        // Result (la que realmente corresponde al dorsal capturado), nunca la del
        // Runner. La Category ya viene precargada por GetByRunnerWithCategoryAsync.
        var categoryId = result.CategoryId;
        var nombreCategoria = categoryId is not null ? result.Category!.NombreCategoria : runner.Category.NombreCategoria;
        var distancia = categoryId is not null ? result.Category!.Distancia : runner.Category.Distancia;

        int? posicionCategoria = null;
        int? totalCategoria = null;

        // Sin categoría en el Result: no hay conteo de categoría (parámetro filler
        // Runner.CategoryId, nunca se lee CategoryAhead/CategoryTotal de esa llamada),
        // pero el placing general SÍ se calcula igual (design.md Decisión 7).
        var counts = await resultRepository.GetPlacingCountsAsync(
            runner.RaceId, categoryId ?? runner.CategoryId, result.TiempoLlegada, result.Id, ct);

        if (categoryId is not null)
        {
            posicionCategoria = counts.CategoryAhead + 1;
            totalCategoria = counts.CategoryTotal;
        }

        var elapsedSeconds = race.RaceStartUtc is { } inicio
            ? (int)(result.TiempoLlegada - inicio).TotalSeconds
            : (int?)null;

        return new PublicRunnerShareDto(
            race.Nombre, slogan, race.FechaCarrera,
            runner.Nombre, runner.Apellidos, runner.Club, runner.Dorsal,
            nombreCategoria, distancia,
            result.TiempoLlegada, elapsedSeconds,
            posicionCategoria, totalCategoria,
            counts.RaceAhead + 1, counts.RaceTotal);
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
