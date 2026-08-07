using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Results;

public class ResultService(
    IResultRepository resultRepository,
    IResultAuditRepository auditRepository,
    IRaceRepository raceRepository,
    IRunnerRepository runnerRepository,
    IRaceDashboardNotifier raceDashboardNotifier) : IResultService
{
    public async Task<ResultDto> CreateAsync(int raceId, CreateResultRequest request, int capturistaId, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);

        // Lookup temprano: si ya hay un Result con este key, devolverlo sin
        // hacer ninguna validación adicional. El cliente que reintenta no
        // espera que validemos de nuevo: espera saber "qué pasó con MI POST".
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await resultRepository.GetByIdempotencyKeyAsync(raceId, idempotencyKey, ct);
            if (existing is not null)
                return ToDto(existing);
        }

        // Captures only when EnCurso (domain rule). RaceStartUtc alone is not enough:
        // a Terminada race may still have RaceStartUtc from when it was running.
        if (race.Estado != RaceStatus.EnCurso)
            throw new ValidationException(
                race.Estado == RaceStatus.Planeada
                    ? "La carrera todavía no arrancó."
                    : $"Solo se pueden capturar llegadas mientras la carrera está EnCurso (estado actual: {race.Estado}).");

        if (race.RaceStartUtc is null)
            throw new ValidationException("La carrera todavía no arrancó.");

        Runner? runner = null;
        if (!string.IsNullOrWhiteSpace(request.Dorsal))
        {
            runner = await runnerRepository.GetByDorsalAsync(raceId, request.Dorsal, ct)
                ?? throw new NotFoundException($"No existe un corredor con el dorsal '{request.Dorsal}' en esta carrera.");

            if (await resultRepository.ExistsByRunnerAsync(raceId, runner.Id, ct: ct))
                throw new ConflictException($"El corredor con dorsal '{request.Dorsal}' ya tiene un tiempo registrado en esta carrera.");
        }

        // El servidor es la única fuente de verdad para el instante de llegada: lo toma de su
        // propio reloj al recibir el request, en vez de confiar en el reloj del celular del
        // juez (que puede estar desincronizado de forma distinta en cada dispositivo).
        var result = new Result
        {
            RaceId = raceId,
            RunnerId = runner?.Id,
            Dorsal = runner?.Dorsal,
            TiempoLlegada = DateTime.UtcNow,
            CategoryId = runner?.CategoryId,
            CapturistaId = capturistaId,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey
        };

        await resultRepository.AddAsync(result, ct);
        try
        {
            await resultRepository.SaveNewResultAsync(ct);
        }
        catch (IdempotencyConflictException) when (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // Race: dos POSTs concurrentes con el mismo key. El primero ya
            // commiteó; el segundo perdió contra el UK. Re-leemos el ganador
            // y devolvemos esa respuesta (idempotente). Si por alguna razón
            // el ganador no aparece en la BD, dejamos burbujear la excepción
            // — es un estado imposible que merece visibilidad.
            var winner = await resultRepository.GetByIdempotencyKeyAsync(raceId, idempotencyKey, ct);
            if (winner is null) throw;
            return ToDto(winner);
        }
        catch (RunnerResultConflictException)
        {
            // A diferencia del conflicto de Idempotency-Key, acá no hay
            // "ganador" que devolver: dos capturas distintas del mismo dorsal
            // es un error real, y el perdedor debe enterarse igual que en el
            // chequeo previo (ExistsByRunnerAsync) — mismo mensaje.
            throw new ConflictException($"El corredor con dorsal '{request.Dorsal}' ya tiene un tiempo registrado en esta carrera.");
        }

        if (runner is not null)
            await RecalculatePositionsAsync(raceId, runner.CategoryId, ct);

        await raceDashboardNotifier.NotifyResultsChangedAsync(raceId, ct);

        var saved = await resultRepository.GetByIdAsync(raceId, result.Id, ct);
        return ToDto(saved ?? result);
    }

    public async Task<PaginatedList<ResultDto>> GetAllByRaceAsync(int raceId, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        await GetRaceOrThrowAsync(raceId, ct);

        var paginated = await resultRepository.GetPaginatedByRaceAsync(raceId, limit, offset, ct);
        return new PaginatedList<ResultDto>(paginated.Items.Select(ToDto).ToList(), paginated.TotalCount);
    }

    public async Task<ResultDto> GetByIdAsync(int raceId, int resultId, CancellationToken ct = default)
    {
        var result = await GetResultOrThrowAsync(raceId, resultId, ct);
        return ToDto(result);
    }

    public async Task<ResultDto> UpdateAsync(int raceId, int resultId, UpdateResultRequest request, int editorId, CancellationToken ct = default)
    {
        var result = await GetResultOrThrowAsync(raceId, resultId, ct);
        var race = await GetRaceOrThrowAsync(raceId, ct);
        ValidateTiempoLlegada(race, request.TiempoLlegada);

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

        await raceDashboardNotifier.NotifyResultsChangedAsync(raceId, ct);

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
            ActorUserId = editorId,
            CampoModificado = campo,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo,
            Razon = razon
        }, ct);
    }

    private async Task RecalculatePositionsAsync(int raceId, int categoryId, CancellationToken ct)
    {
        // enlaces-publicos-resultados design.md Decisión 4: desempate (TiempoLlegada, Id)
        // — antes solo ordenaba por TiempoLlegada, así que dos resultados con el mismo
        // instante recibían una Posicion arbitraria (orden estable = orden de llegada de
        // la query, no reproducible). Debe coincidir EXACTO con el desempate de
        // GetPlacingCountsAsync (ResultRepository) para que esta Posicion guardada nunca
        // contradiga el placing derivado que ve el enlace público de detalle del corredor.
        var results = await resultRepository.GetAllByCategoryAsync(raceId, categoryId, ct);
        // Solo Valido cuenta para posiciones. Un tiempo en Controversia es dudoso por
        // definición; uno Anulado no debería haber estado ahí. Premiar cualquiera de
        // los dos sería premiar un dato que el propio sistema marcó como no confiable.
        var ordered = results
            .Where(r => r.Estado == ResultEstado.Valido)
            .OrderBy(r => r.TiempoLlegada).ThenBy(r => r.Id)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Posicion = i + 1;

        await resultRepository.SaveChangesAsync(ct);
    }

    private async Task<Race> GetRaceOrThrowAsync(int raceId, CancellationToken ct) =>
        await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

    // Margen de tolerancia por diferencias de reloj entre el dispositivo del
    // capturista y el servidor — no rechazar un tiempo real por unos segundos
    // de desfase.
    private const int FutureToleranceMinutes = 5;

    private static void ValidateTiempoLlegada(Race race, DateTime tiempoLlegada)
    {
        if (race.RaceStartUtc is { } inicio && tiempoLlegada < inicio)
            throw new ValidationException("El tiempo de llegada no puede ser anterior al inicio de la carrera.");

        if (tiempoLlegada > DateTime.UtcNow.AddMinutes(FutureToleranceMinutes))
            throw new ValidationException("El tiempo de llegada no puede ser una fecha futura.");
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
        audit.ActorUserId,
        audit.CampoModificado,
        audit.ValorAnterior,
        audit.ValorNuevo,
        audit.Razon,
        audit.CreatedAt);
}
