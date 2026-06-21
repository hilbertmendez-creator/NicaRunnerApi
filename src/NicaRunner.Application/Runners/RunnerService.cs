using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Runners.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Runners;

public class RunnerService(
    IRunnerRepository runnerRepository,
    IRaceRepository raceRepository,
    IRaceCategoryRepository categoryRepository) : IRunnerService
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
