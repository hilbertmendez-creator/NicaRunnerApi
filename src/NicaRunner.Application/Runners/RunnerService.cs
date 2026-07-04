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
        var race = await GetRaceOrThrowAsync(raceId, ct);
        var category = await EnsureCategoryBelongsToRaceAsync(raceId, request.CategoryId, ct);

        if (await runnerRepository.DorsalExistsAsync(raceId, request.Dorsal, ct: ct))
            throw new ConflictException($"Ya existe un corredor con el dorsal '{request.Dorsal}' en esta carrera.");

        var edad = ResolveEdad(request.FechaNacimiento, request.Edad, race.FechaCarrera);
        EnsureAgeMatchesCategoryOrThrow(edad, category);

        var runner = new Runner
        {
            RaceId = raceId,
            Nombre = request.Nombre,
            Apellidos = request.Apellidos,
            Dorsal = request.Dorsal,
            Telefono = request.Telefono,
            Email = request.Email,
            Sexo = request.Sexo,
            Club = request.Club,
            FechaNacimiento = request.FechaNacimiento,
            Edad = edad,
            CategoryId = request.CategoryId
        };

        await runnerRepository.AddAsync(runner, ct);
        await runnerRepository.SaveChangesAsync(ct);

        var saved = await runnerRepository.GetByIdAsync(raceId, runner.Id, ct);
        return ToDto(saved ?? runner);
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
        var race = await GetRaceOrThrowAsync(raceId, ct);
        var runner = await GetRunnerOrThrowAsync(raceId, runnerId, ct);
        var category = await EnsureCategoryBelongsToRaceAsync(raceId, request.CategoryId, ct);

        if (await runnerRepository.DorsalExistsAsync(raceId, request.Dorsal, runnerId, ct))
            throw new ConflictException($"Ya existe un corredor con el dorsal '{request.Dorsal}' en esta carrera.");

        var edad = ResolveEdad(request.FechaNacimiento, request.Edad, race.FechaCarrera);
        EnsureAgeMatchesCategoryOrThrow(edad, category);

        runner.Nombre = request.Nombre;
        runner.Apellidos = request.Apellidos;
        runner.Dorsal = request.Dorsal;
        runner.Telefono = request.Telefono;
        runner.Email = request.Email;
        runner.Sexo = request.Sexo;
        runner.Club = request.Club;
        runner.FechaNacimiento = request.FechaNacimiento;
        runner.Edad = edad;
        runner.CategoryId = request.CategoryId;

        await runnerRepository.SaveChangesAsync(ct);

        var saved = await runnerRepository.GetByIdAsync(raceId, runnerId, ct);
        return ToDto(saved ?? runner);
    }

    public async Task DeleteAsync(int raceId, int runnerId, CancellationToken ct = default)
    {
        var runner = await GetRunnerOrThrowAsync(raceId, runnerId, ct);
        runnerRepository.Remove(runner);
        await runnerRepository.SaveChangesAsync(ct);
    }

    public async Task<byte[]> GenerateImportTemplateAsync(int raceId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var categories = await categoryRepository.GetAllByRaceAsync(raceId, ct);
        return excelRunnerParser.GenerateTemplate(categories);
    }

    public async Task<ImportRunnersResultDto> ImportFromExcelAsync(int raceId, Stream excelStream, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);

        List<ParsedRunnerRow> rows;
        try
        {
            rows = excelRunnerParser.Parse(excelStream);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // ClosedXML no documenta un tipo de excepción propio para "no es un
            // OOXML válido" — atrapamos genérico (mismo boundary que ya cruza
            // ResultRepository con DbUpdateException, acá el origen es una
            // librería de parseo en vez de EF) y devolvemos un 400 claro en vez
            // de dejar que se escape como 500 sin manejar.
            throw new ValidationException("El archivo no es un Excel válido (.xlsx) o está dañado.");
        }

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

            if (row.FechaNacimiento is null)
                reasons.Add("Fecha de nacimiento inválida o vacía");

            Sexo? sexo = null;
            if (!string.IsNullOrWhiteSpace(row.Sexo))
            {
                if (TryParseSexo(row.Sexo, out var parsedSexo))
                    sexo = parsedSexo;
                else
                    reasons.Add($"Sexo '{row.Sexo}' inválido (use M o F)");
            }

            Category? category = null;
            if (string.IsNullOrWhiteSpace(row.Categoria))
                reasons.Add("Categoría vacía");
            else if (!categoriesByName.TryGetValue(Normalize(row.Categoria), out category))
                reasons.Add($"La categoría '{row.Categoria}' no existe en esta carrera");

            if (reasons.Count == 0 && !string.IsNullOrWhiteSpace(row.Dorsal) &&
                (existingDorsals.Contains(row.Dorsal) || seenDorsals.Contains(row.Dorsal)))
                reasons.Add($"El dorsal '{row.Dorsal}' ya existe en esta carrera o está duplicado en el archivo");

            int edad = 0;
            if (reasons.Count == 0 && category is not null)
            {
                edad = CalculateEdad(row.FechaNacimiento!.Value, race.FechaCarrera);
                if (!IsAgeValidForCategory(edad, category))
                    reasons.Add($"La edad ({edad}) no corresponde al rango de la categoría '{category.NombreCategoria}' ({category.EdadMinima}-{category.EdadMaxima})");
            }

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
                Apellidos = row.Apellidos?.Trim(),
                Dorsal = row.Dorsal.Trim(),
                Telefono = row.Telefono,
                Email = row.Email,
                Sexo = sexo,
                Club = row.Club,
                FechaNacimiento = row.FechaNacimiento,
                Edad = edad,
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

    private static bool TryParseSexo(string value, out Sexo sexo)
    {
        switch (value.Trim().ToUpperInvariant())
        {
            case "M":
            case "MASCULINO":
                sexo = Sexo.M;
                return true;
            case "F":
            case "FEMENINO":
                sexo = Sexo.F;
                return true;
            default:
                sexo = default;
                return false;
        }
    }

    private static int ResolveEdad(DateTime? fechaNacimiento, int? edad, DateTime fechaCarrera)
    {
        if (fechaNacimiento is not null)
            return CalculateEdad(fechaNacimiento.Value, fechaCarrera);

        if (edad is not null)
            return edad.Value;

        throw new ValidationException("Debe indicar la fecha de nacimiento o, en su defecto, la edad del corredor.");
    }

    private static int CalculateEdad(DateTime fechaNacimiento, DateTime asOf)
    {
        var edad = asOf.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > asOf.Date.AddYears(-edad))
            edad--;
        return Math.Max(edad, 0);
    }

    private static bool IsAgeValidForCategory(int edad, Category category) =>
        edad >= category.EdadMinima && edad <= category.EdadMaxima;

    private static void EnsureAgeMatchesCategoryOrThrow(int edad, Category category)
    {
        if (!IsAgeValidForCategory(edad, category))
            throw new ValidationException(
                $"La edad ({edad}) no corresponde al rango de la categoría '{category.NombreCategoria}' ({category.EdadMinima}-{category.EdadMaxima}).");
    }

    private async Task EnsureRaceExistsAsync(int raceId, CancellationToken ct)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");
    }

    private async Task<Race> GetRaceOrThrowAsync(int raceId, CancellationToken ct) =>
        await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

    private async Task<Category> EnsureCategoryBelongsToRaceAsync(int raceId, int categoryId, CancellationToken ct) =>
        await categoryRepository.GetByIdAsync(raceId, categoryId, ct)
            ?? throw new NotFoundException($"No existe la categoría con id {categoryId} en la carrera {raceId}.");

    private async Task<Runner> GetRunnerOrThrowAsync(int raceId, int runnerId, CancellationToken ct) =>
        await runnerRepository.GetByIdAsync(raceId, runnerId, ct)
            ?? throw new NotFoundException($"No existe el corredor con id {runnerId} en la carrera {raceId}.");

    private static RunnerDto ToDto(Runner runner) => new(
        runner.Id,
        runner.RaceId,
        runner.Nombre,
        runner.Apellidos,
        runner.Dorsal,
        runner.Telefono,
        runner.Email,
        runner.Sexo,
        runner.Club,
        runner.FechaNacimiento,
        runner.Edad,
        runner.CategoryId,
        runner.Category?.NombreCategoria ?? string.Empty,
        runner.Category?.Distancia ?? 0m,
        runner.CreatedAt);
}
