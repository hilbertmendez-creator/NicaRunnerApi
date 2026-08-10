using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Dashboard.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Dashboard;

public class DashboardService(
    IRaceRepository raceRepository,
    IRaceCategoryRepository categoryRepository,
    IRunnerRepository runnerRepository,
    IResultRepository resultRepository) : IDashboardService
{
    public async Task<RaceDashboardDto> GetDashboardAsync(int raceId, CancellationToken ct = default)
    {
        var race = await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

        var categories = await categoryRepository.GetAllByRaceAsync(raceId, ct);
        var runners = await runnerRepository.GetAllByRaceAsync(raceId, ct);
        var results = await resultRepository.GetAllByRaceAsync(raceId, ct);

        var runnersById = runners.ToDictionary(r => r.Id);
        var categoriesById = categories.ToDictionary(c => c.Id);

        var categorias = categories
            .Select(category =>
            {
                var inscritos = runners.Count(r => r.CategoryId == category.Id);
                var conTiempo = results.Count(r => r.CategoryId == category.Id);
                return new CategoryProgressDto(category.Id, category.NombreCategoria, inscritos, conTiempo, inscritos - conTiempo);
            })
            .ToList();

        var ultimosResultados = results
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new RecentResultDto(
                r.Id,
                r.Dorsal ?? "(sin asignar)",
                r.RunnerId is { } rid && runnersById.TryGetValue(rid, out var runner) ? runner.Nombre : "(sin asignar)",
                r.TiempoLlegada,
                r.Posicion,
                r.CategoryId is { } cid && categoriesById.TryGetValue(cid, out var category) ? category.NombreCategoria : "(sin asignar)",
                r.CapturistaId))
            .ToList();

        var tiempoEnCursoSegundos = race.Estado == RaceStatus.EnCurso && race.RaceStartUtc is { } startUtc
            ? (int)(DateTime.UtcNow - startUtc).TotalSeconds
            : (int?)null;

        var ritmoPromedioSegundosPorKm = CalcularRitmoPromedioSegundosPorKm(race, results, categoriesById);

        return new RaceDashboardDto(
            race.Id,
            race.Nombre,
            race.Estado,
            runners.Count,
            results.Count,
            runners.Count - results.Count,
            categorias,
            ultimosResultados,
            tiempoEnCursoSegundos,
            ritmoPromedioSegundosPorKm);
    }

    private static int? CalcularRitmoPromedioSegundosPorKm(
        Race race,
        List<Result> results,
        Dictionary<int, Category> categoriesById)
    {
        if (race.RaceStartUtc is not { } startUtc)
            return null;

        var ritmos = results
            .Where(r => r.Estado == ResultEstado.Valido && r.CategoryId is not null)
            .Select(r => categoriesById.TryGetValue(r.CategoryId!.Value, out var category) && category.Distancia > 0
                ? (double?)(r.TiempoLlegada - startUtc).TotalSeconds / (double)category.Distancia
                : null)
            .Where(ritmo => ritmo is not null)
            .Select(ritmo => ritmo!.Value)
            .ToList();

        return ritmos.Count == 0 ? null : (int)ritmos.Average();
    }

    public async Task<List<CategoryStandingsDto>> GetStandingsAsync(int raceId, CancellationToken ct = default)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");

        var categories = await categoryRepository.GetAllByRaceAsync(raceId, ct);
        var runnersById = (await runnerRepository.GetAllByRaceAsync(raceId, ct)).ToDictionary(r => r.Id);
        var results = await resultRepository.GetAllByRaceAsync(raceId, ct);

        return categories
            .Select(category => new CategoryStandingsDto(
                category.Id,
                category.NombreCategoria,
                category.Distancia,
                results
                    .Where(r => r.CategoryId == category.Id && r.RunnerId is not null)
                    .OrderBy(r => r.Posicion)
                    .Select(r => new RunnerStandingDto(
                        r.RunnerId!.Value,
                        runnersById.TryGetValue(r.RunnerId.Value, out var runner) ? runner.Nombre : "(desconocido)",
                        r.Dorsal ?? string.Empty,
                        r.Posicion,
                        r.TiempoLlegada))
                    .ToList()))
            .ToList();
    }
}
