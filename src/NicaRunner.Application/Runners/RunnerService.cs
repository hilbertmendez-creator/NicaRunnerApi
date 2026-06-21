using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Runners.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Runners;

public class RunnerService(
    IRunnerRepository runnerRepository,
    IRaceRepository raceRepository,
    IRaceCategoryRepository categoryRepository,
    IExcelRunnerParser excelRunnerParser) : IRunnerService
{
    public async Task<RunnerDto> CreateAsync(int raceId, CreateRunnerRequest request, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);
        await EnsureCategoryBelongsToRaceAsync(raceId, request.CategoryId, ct);

        if (await runnerRepository.DorsalExistsAsync(raceId, request.Dorsal, ct: ct))
            throw new ConflictException($"Ya existe un corredor con el dorsal '{request.Dorsal}' en esta carrera.");

        var runner = new Runner
        {
            RaceId = raceId,
            Nombre = request.Nombre,
            Dorsal = request.Dorsal,
            Telefono = request.Telefono,
            Email = request.Email,
            Edad = request.Edad,
            CategoryId = request.CategoryId
        };

        await runnerRepository.AddAsync(runner, ct);
        await runnerRepository.SaveChangesAsync(ct);

        return ToDto(runner);
    }

    public async Task<List<RunnerDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var runners = await runnerRepository.GetAllByRaceAsync(raceId, ct);
        return runners.Select(ToDto).ToList();
    }

    public async Task<RunnerDto> GetByIdAsync(int raceId, int runnerId, CancellationToken ct = default)
    {
        var runner = await GetRunnerOrThrowAsync(raceId, runnerId, ct);
        return ToDto(runner);
    }

    public async Task<RunnerDto> UpdateAsync(int raceId, int runnerId, UpdateRunnerRequest request, CancellationToken ct = default)
    {
        var runner = await GetRunnerOrThrowAsync(raceId, runnerId, ct);
        await EnsureCategoryBelongsToRaceAsync(raceId, request.CategoryId, ct);

        if (await runnerRepository.DorsalExistsAsync(raceId, request.Dorsal, runnerId, ct))
            throw new ConflictException($"Ya existe un corredor con el dorsal '{request.Dorsal}' en esta carrera.");

        runner.Nombre = request.Nombre;
        runner.Dorsal = request.Dorsal;
        runner.Telefono = request.Telefono;
        runner.Email = request.Email;
        runner.Edad = request.Edad;
        runner.CategoryId = request.CategoryId;

        await runnerRepository.SaveChangesAsync(ct);
        return ToDto(runner);
    }

    public async Task DeleteAsync(int raceId, int runnerId, CancellationToken ct = default)
    {
        var runner = await GetRunnerOrThrowAsync(raceId, runnerId, ct);
        runnerRepository.Remove(runner);
        await runnerRepository.SaveChangesAsync(ct);
    }

    public async Task<ImportRunnersResultDto> ImportFromExcelAsync(int raceId, Stream excelStream, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var rows = excelRunnerParser.Parse(excelStream);

        var categoriesByName = (await categoryRepository.GetAllByRaceAsync(raceId, ct))
            .GroupBy(c => Normalize(c.NombreCategoria))
            .ToDictionary(g => g.Key, g => g.First());

        var existingDorsals = (await runnerRepository.GetAllByRaceAsync(raceId, ct))
            .Select(r => r.Dorsal)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenDorsals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<ImportRunnerError>();
        var toAdd = new List<Runner>();

        foreach (var row in rows)
        {
            var reasons = new List<string>();

            if (string.IsNullOrWhiteSpace(row.Nombre))
                reasons.Add("Nombre vacío");

            if (string.IsNullOrWhiteSpace(row.Dorsal))
                reasons.Add("Dorsal vacío");

            if (row.Edad is null)
                reasons.Add("Edad inválida o vacía");

            RaceCategory? category = null;
            if (string.IsNullOrWhiteSpace(row.Categoria))
                reasons.Add("Categoría vacía");
            else if (!categoriesByName.TryGetValue(Normalize(row.Categoria), out category))
                reasons.Add($"La categoría '{row.Categoria}' no existe en esta carrera");

            if (reasons.Count == 0 && !string.IsNullOrWhiteSpace(row.Dorsal) &&
                (existingDorsals.Contains(row.Dorsal) || seenDorsals.Contains(row.Dorsal)))
                reasons.Add($"El dorsal '{row.Dorsal}' ya existe en esta carrera o está duplicado en el archivo");

            if (reasons.Count > 0)
            {
                errors.Add(new ImportRunnerError(row.Fila, string.Join("; ", reasons)));
                continue;
            }

            seenDorsals.Add(row.Dorsal);
            toAdd.Add(new Runner
            {
                RaceId = raceId,
                Nombre = row.Nombre.Trim(),
                Dorsal = row.Dorsal.Trim(),
                Telefono = row.Telefono,
                Email = row.Email,
                Edad = row.Edad!.Value,
                CategoryId = category!.Id
            });
        }

        if (toAdd.Count > 0)
        {
            await runnerRepository.AddRangeAsync(toAdd, ct);
            await runnerRepository.SaveChangesAsync(ct);
        }

        return new ImportRunnersResultDto(rows.Count, toAdd.Count, errors);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private async Task EnsureRaceExistsAsync(int raceId, CancellationToken ct)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");
    }

    private async Task EnsureCategoryBelongsToRaceAsync(int raceId, int categoryId, CancellationToken ct)
    {
        if (await categoryRepository.GetByIdAsync(raceId, categoryId, ct) is null)
            throw new NotFoundException($"No existe la categoría con id {categoryId} en la carrera {raceId}.");
    }

    private async Task<Runner> GetRunnerOrThrowAsync(int raceId, int runnerId, CancellationToken ct) =>
        await runnerRepository.GetByIdAsync(raceId, runnerId, ct)
            ?? throw new NotFoundException($"No existe el corredor con id {runnerId} en la carrera {raceId}.");

    private static RunnerDto ToDto(Runner runner) => new(
        runner.Id,
        runner.RaceId,
        runner.Nombre,
        runner.Dorsal,
        runner.Telefono,
        runner.Email,
        runner.Edad,
        runner.CategoryId,
        runner.CreatedAt);
}
