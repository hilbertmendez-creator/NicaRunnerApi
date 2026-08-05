using System.ComponentModel.DataAnnotations;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Runners.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Runners;

public class RunnerService(
    IRunnerRepository runnerRepository,
    IRaceRepository raceRepository,
    IRaceCategoryRepository categoryRepository,
    IExcelRunnerParser excelRunnerParser,
    IReservedDorsalRepository reservedDorsalRepository) : IRunnerService
{
    public async Task<RunnerDto> CreateAsync(int raceId, CreateRunnerRequest request, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        var category = await EnsureCategoryBelongsToRaceAsync(raceId, request.CategoryId, ct);

        // design.md D9: el Dorsal manual sigue sin regla de formato — solo unicidad
        // (normalizada, D11) y no-reservado (D8) se validan acá.
        await EnsureDorsalAvailableOrThrowAsync(raceId, request.Dorsal, excludeRunnerId: null, ct);

        var edad = ResolveEdad(request.FechaNacimiento, request.Edad, race.FechaCarrera);
        EnsureAgeMatchesCategoryOrThrow(edad, category);

        var runner = new Runner
        {
            RaceId = raceId,
            Nombre = request.Nombre,
            Apellidos = request.Apellidos,
            Dorsal = request.Dorsal,
            DorsalNormalizado = DorsalNormalizer.Normalize(request.Dorsal),
            Telefono = request.Telefono,
            Email = request.Email,
            Sexo = request.Sexo,
            Club = request.Club,
            FechaNacimiento = request.FechaNacimiento,
            Edad = edad,
            CategoryId = request.CategoryId,
            PublicShareKey = ShareKeyGenerator.Generate()
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

        // design.md D10: el chequeo de ReservedDorsal solo corre cuando el dorsal
        // realmente cambia (numéricamente, ignorando ceros a la izquierda) — mismo grano
        // que el excludeRunnerId de arriba. Editar un campo no relacionado en un corredor
        // que YA tenía ese dorsal (aunque después se haya reservado) nunca debe fallar.
        var normalizedNew = DorsalNormalizer.Normalize(request.Dorsal);
        if (normalizedNew != runner.DorsalNormalizado &&
            await reservedDorsalRepository.IsReservedAsync(raceId, request.Dorsal, ct))
        {
            throw new ConflictException($"El dorsal '{request.Dorsal}' está reservado y no puede asignarse.");
        }

        var edad = ResolveEdad(request.FechaNacimiento, request.Edad, race.FechaCarrera);
        EnsureAgeMatchesCategoryOrThrow(edad, category);

        runner.Nombre = request.Nombre;
        runner.Apellidos = request.Apellidos;
        runner.Dorsal = request.Dorsal;
        runner.DorsalNormalizado = normalizedNew;
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

    public async Task<Runner> CreateFromRegistrationAsync(Registration registration, string dorsal, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dorsal))
            throw new Common.Exceptions.ValidationException("Debe indicar un dorsal para confirmar la inscripción.");

        var race = await GetRaceOrThrowAsync(registration.RaceId, ct);

        await EnsureDorsalAvailableOrThrowAsync(registration.RaceId, dorsal, excludeRunnerId: null, ct);

        var edad = EdadCalculator.AtRaceDate(registration.FechaNacimiento, race.FechaCarrera);

        var runner = new Runner
        {
            RaceId = registration.RaceId,
            Nombre = registration.Nombre,
            Apellidos = registration.Apellidos,
            Dorsal = dorsal.Trim(),
            DorsalNormalizado = DorsalNormalizer.Normalize(dorsal),
            Telefono = registration.Telefono,
            Email = registration.Email,
            Sexo = registration.Sexo,
            Club = registration.Club,
            FechaNacimiento = registration.FechaNacimiento,
            Edad = edad,
            CategoryId = registration.RaceCategory.CategoryId,
            PublicShareKey = ShareKeyGenerator.Generate()
        };

        // Deliberadamente sin SaveChangesAsync acá — ver el comentario en
        // IRunnerService.CreateFromRegistrationAsync.
        await runnerRepository.AddAsync(runner, ct);
        return runner;
    }

    // design.md D8/D9: chequeo compartido entre CreateAsync y CreateFromRegistrationAsync
    // (confirm individual/bulk) — reservado primero, duplicado (normalizado) después.
    // UpdateAsync no lo usa tal cual porque su chequeo de reservado es condicional (D10).
    private async Task EnsureDorsalAvailableOrThrowAsync(int raceId, string dorsal, int? excludeRunnerId, CancellationToken ct)
    {
        if (await reservedDorsalRepository.IsReservedAsync(raceId, dorsal, ct))
            throw new ConflictException($"El dorsal '{dorsal}' está reservado y no puede asignarse.");

        if (await runnerRepository.DorsalExistsAsync(raceId, dorsal, excludeRunnerId, ct))
            throw new ConflictException($"Ya existe un corredor con el dorsal '{dorsal}' en esta carrera.");
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
            throw new Common.Exceptions.ValidationException("El archivo no es un Excel válido (.xlsx) o está dañado.");
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

            if (!string.IsNullOrWhiteSpace(row.Email) && !new EmailAddressAttribute().IsValid(row.Email))
                reasons.Add($"Email '{row.Email}' no es válido");

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
                edad = EdadCalculator.AtRaceDate(row.FechaNacimiento!.Value, race.FechaCarrera);
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
                // public-runner-registration-manual-payment (D11): sin esto, dos o más
                // filas importadas en el mismo lote comparten DorsalNormalizado="" (el
                // default de la entidad) y el índice único aditivo IX_Runners_RaceId_
                // DorsalNormalizado rechazaría el SaveChangesAsync entero, no solo la fila
                // conflictiva — no está en tasks.md 2.x explícitamente, pero "sets
                // DorsalNormalizado on every write" (design.md, File Changes) es general.
                DorsalNormalizado = DorsalNormalizer.Normalize(row.Dorsal.Trim()),
                Telefono = row.Telefono,
                Email = row.Email,
                Sexo = sexo,
                Club = row.Club,
                FechaNacimiento = row.FechaNacimiento,
                Edad = edad,
                CategoryId = category!.Id,
                PublicShareKey = ShareKeyGenerator.Generate()
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
            return EdadCalculator.AtRaceDate(fechaNacimiento.Value, fechaCarrera);

        if (edad is not null)
            return edad.Value;

        throw new Common.Exceptions.ValidationException("Debe indicar la fecha de nacimiento o, en su defecto, la edad del corredor.");
    }

    private static bool IsAgeValidForCategory(int edad, Category category) =>
        edad >= category.EdadMinima && edad <= category.EdadMaxima;

    private static void EnsureAgeMatchesCategoryOrThrow(int edad, Category category)
    {
        if (!IsAgeValidForCategory(edad, category))
            throw new Common.Exceptions.ValidationException(
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
