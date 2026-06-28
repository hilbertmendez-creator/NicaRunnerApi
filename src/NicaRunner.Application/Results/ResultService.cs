using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Results;

public class ResultService(
    IResultRepository resultRepository,
    IResultAuditRepository auditRepository,
    IRaceRepository raceRepository,
    IRunnerRepository runnerRepository) : IResultService
{
    public async Task<ResultDto> CreateAsync(int raceId, CreateResultRequest request, int capturistaId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        Runner? runner = null;
        if (!string.IsNullOrWhiteSpace(request.Dorsal))
        {
            runner = await runnerRepository.GetByDorsalAsync(raceId, request.Dorsal, ct)
                ?? throw new NotFoundException($"No existe un corredor con el dorsal '{request.Dorsal}' en esta carrera.");

            if (await resultRepository.ExistsByRunnerAsync(raceId, runner.Id, ct: ct))
                throw new ConflictException($"El corredor con dorsal '{request.Dorsal}' ya tiene un tiempo registrado en esta carrera.");
        }

        var result = new Result
        {
            RaceId = raceId,
            RunnerId = runner?.Id,
            Dorsal = runner?.Dorsal,
            TiempoLlegada = request.TiempoLlegada,
            CategoryId = runner?.CategoryId,
            CapturistaId = capturistaId
        };

        await resultRepository.AddAsync(result, ct);
        await resultRepository.SaveChangesAsync(ct);

        if (runner is not null)
            await RecalculatePositionsAsync(raceId, runner.CategoryId, ct);

        var saved = await resultRepository.GetByIdAsync(raceId, result.Id, ct);
        return ToDto(saved ?? result);
    }

    public async Task<List<ResultDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var results = await resultRepository.GetAllByRaceAsync(raceId, ct);
        return results.Select(ToDto).ToList();
    }

    public async Task<ResultDto> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default)
    {
        var result = await GetResultOrThrowAsync(raceId, resultId, ct);
        return ToDto(result);
    }

    public async Task<ResultDto> UpdateAsync(int raceId, int resultId, UpdateResultRequest request, int editorId, CancellationToken ct = default)
    {
        var result = await GetResultOrThrowAsync(raceId, resultId, ct);

        var runner = await runnerRepository.GetByDorsalAsync(raceId, request.Dorsal, ct)
            ?? throw new NotFoundException($"No existe un corredor con el dorsal '{request.Dorsal}' en esta carrera.");

        if (runner.Id != result.RunnerId && await resultRepository.ExistsByRunnerAsync(raceId, runner.Id, resultId, ct))
            throw new ConflictException($"El corredor con dorsal '{request.Dorsal}' ya tiene un tiempo registrado en esta carrera.");

        var oldCategoryId = result.CategoryId;

        await RegisterAuditIfChangedAsync(result.Id, editorId, "Dorsal", result.Dorsal ?? "(sin asignar)", request.Dorsal, request.Razon, ct);
        await RegisterAuditIfChangedAsync(result.Id, editorId, "TiempoLlegada", result.TiempoLlegada.ToString("O"), request.TiempoLlegada.ToString("O"), request.Razon, ct);

        result.Dorsal = request.Dorsal;
        result.TiempoLlegada = request.TiempoLlegada;
        result.RunnerId = runner.Id;
        result.CategoryId = runner.CategoryId;
        result.UpdatedAt = DateTime.UtcNow;

        await resultRepository.SaveChangesAsync(ct);

        if (oldCategoryId is not null)
            await RecalculatePositionsAsync(raceId, oldCategoryId.Value, ct);
        if (runner.CategoryId != oldCategoryId)
            await RecalculatePositionsAsync(raceId, runner.CategoryId, ct);

        var saved = await resultRepository.GetByIdAsync(raceId, result.Id, ct);
        return ToDto(saved ?? result);
    }

    public async Task<List<ResultAuditDto>> GetAuditAsync(int raceId, int resultId, CancellationToken ct = default)
    {
        await GetResultOrThrowAsync(raceId, resultId, ct);

        var entries = await auditRepository.GetAllByResultAsync(resultId, ct);
        return entries
            .OrderByDescending(a => a.CreatedAt)
            .Select(ToAuditDto)
            .ToList();
    }

    private async Task RegisterAuditIfChangedAsync(
        int resultId, int editorId, string campo, string valorAnterior, string valorNuevo, string razon, CancellationToken ct)
    {
        if (valorAnterior == valorNuevo)
            return;

        await auditRepository.AddAsync(new ResultAudit
        {
            ResultId = resultId,
            AdminId = editorId,
            CampoModificado = campo,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo,
            Razon = razon
        }, ct);
    }

    private async Task RecalculatePositionsAsync(int raceId, int categoryId, CancellationToken ct)
    {
        var results = await resultRepository.GetAllByCategoryAsync(raceId, categoryId, ct);
        var ordered = results.OrderBy(r => r.TiempoLlegada).ToList();

        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Posicion = i + 1;

        await resultRepository.SaveChangesAsync(ct);
    }

    private async Task EnsureRaceExistsAsync(int raceId, CancellationToken ct)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");
    }

    private async Task<Result> GetResultOrThrowAsync(int raceId, int resultId, CancellationToken ct) =>
        await resultRepository.GetByIdAsync(raceId, resultId, ct)
            ?? throw new NotFoundException($"No existe el resultado con id {resultId} en la carrera {raceId}.");

    private static ResultDto ToDto(Result result) => new(
        result.Id,
        result.RaceId,
        result.RunnerId,
        result.Runner?.Nombre ?? string.Empty,
        result.Dorsal,
        result.TiempoLlegada,
        result.Posicion,
        result.CategoryId,
        result.Category?.NombreCategoria ?? string.Empty,
        result.CapturistaId,
        result.Capturista?.Nombre ?? string.Empty,
        result.CreatedAt,
        result.UpdatedAt);

    private static ResultAuditDto ToAuditDto(ResultAudit audit) => new(
        audit.Id,
        audit.ResultId,
        audit.AdminId,
        audit.CampoModificado,
        audit.ValorAnterior,
        audit.ValorNuevo,
        audit.Razon,
        audit.CreatedAt);
}
