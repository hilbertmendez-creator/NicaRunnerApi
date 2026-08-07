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
    IRaceDashboardNotifier raceDashboardNotifier,
    IRaceCategoryRepository raceCategoryRepository) : IResultService
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

        // El servidor es la única fuente de verdad para el instante de llegada: lo toma de su
        // propio reloj al recibir el request, en vez de confiar en el reloj del celular del
        // juez (que puede estar desincronizado de forma distinta en cada dispositivo).
        var result = new Result
        {
            RaceId = raceId,
            TiempoLlegada = DateTime.UtcNow,
            CapturistaId = capturistaId,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey
        };

        await resultRepository.AddAsync(result, ct);

        // Primer save: SIN dorsal todavía, así que la única UK que puede chocar acá es
        // (RaceId, IdempotencyKey) — RunnerId sigue null, esa UK no puede violarse en
        // este INSERT. Necesitamos el Id real de `result` ANTES de que
        // TryResolveDorsalAsync pueda escribir un ResultAudit o forzar un SaveChanges
        // intermedio vía RecalculatePositionsAsync (rama de disputa) — un ResultAudit
        // con ResultId=0 viola su FK real contra Results, y eso pasaba ANTES de este
        // fix porque TryResolveDorsalAsync corría antes que este AddAsync/save.
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

        if (!string.IsNullOrWhiteSpace(request.Dorsal))
            await TryResolveDorsalAsync(result, request.Dorsal, raceId, capturistaId, ct);

        // Segundo save: persiste lo que TryResolveDorsalAsync haya dejado en `result`
        // — Dorsal/RunnerId/CategoryId en el camino feliz, o Estado/DorsalPropuesto/
        // DisputeGroupId/DisputeMotivo si abrió una disputa. Reusa SaveNewResultAsync
        // por su traducción de la UK de RunnerId a RunnerResultConflictException — el
        // nombre quedó de cuando esto era un solo insert, pero la traducción de
        // constraint-violation no depende de que sea un INSERT o un UPDATE.
        try
        {
            await resultRepository.SaveNewResultAsync(ct);
        }
        catch (RunnerResultConflictException)
        {
            // A diferencia del conflicto de Idempotency-Key, acá no hay
            // "ganador" que devolver: dos capturas distintas del mismo dorsal
            // es un error real, y el perdedor debe enterarse igual que en el
            // chequeo previo (ExistsByRunnerAsync dentro de TryResolveDorsalAsync)
            // — mismo mensaje.
            throw new ConflictException($"El corredor con dorsal '{request.Dorsal}' ya tiene un tiempo registrado en esta carrera.");
        }

        if (result.CategoryId is { } categoryId)
            await RecalculatePositionsAsync(raceId, categoryId, ct);

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

        var oldCategoryId = result.CategoryId;
        var oldDorsal = result.Dorsal;

        await RegisterAuditIfChangedAsync(result.Id, editorId, "TiempoLlegada", result.TiempoLlegada.ToString("O"), request.TiempoLlegada.ToString("O"), request.Razon, ct);
        result.TiempoLlegada = request.TiempoLlegada;
        result.UpdatedAt = DateTime.UtcNow;

        var opened = await TryResolveDorsalAsync(result, request.Dorsal, raceId, editorId, ct);

        if (!opened)
        {
            await RegisterAuditIfChangedAsync(result.Id, editorId, "Dorsal", oldDorsal ?? "(sin asignar)", request.Dorsal, request.Razon, ct);

            await resultRepository.SaveChangesAsync(ct);

            if (oldCategoryId is not null)
                await RecalculatePositionsAsync(raceId, oldCategoryId.Value, ct);
            if (result.CategoryId != oldCategoryId)
                await RecalculatePositionsAsync(raceId, result.CategoryId!.Value, ct);
        }
        else
        {
            await resultRepository.SaveChangesAsync(ct);
        }

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

    /// <summary>
    /// El primer resultado de un grupo de disputa usa SU PROPIO Id como DisputeGroupId
    /// — no hace falta una tabla ni una secuencia nueva, y es estable: dado el Id del
    /// resultado más viejo del grupo, cualquiera puede reconstruir el grupo entero con
    /// WHERE DisputeGroupId = ese Id.
    /// </summary>
    private static int ResolveDisputeGroupId(Result existing) =>
        existing.DisputeGroupId ?? existing.Id;

    /// <summary>
    /// Corazón de F2/F3. Se llama con un Result que TODAVÍA no tiene Dorsal/CategoryId
    /// aplicados (uno recién creado, o uno existente al que se le está por asignar).
    /// Si el dorsal está libre y la categoría acepta captura ahora mismo, aplica
    /// Dorsal/CategoryId de una y deja Estado=Valido — camino feliz, sin tocar nada
    /// de disputa. Si no, deja el resultado en Controversia con la intención guardada
    /// en DorsalPropuesto, SIN tocar Dorsal/CategoryId, y devuelve true para que el
    /// caller sepa que no debe seguir con el resto del flujo normal (recálculo de
    /// posiciones de una categoría a la que este resultado nunca entró).
    /// </summary>
    private async Task<bool> TryResolveDorsalAsync(
        Result target, string dorsal, int raceId, int actorUserId, CancellationToken ct)
    {
        var runner = await runnerRepository.GetByDorsalAsync(raceId, dorsal, ct)
            ?? throw new NotFoundException($"No existe un corredor con el dorsal '{dorsal}' en esta carrera.");

        var yaOcupado = await resultRepository.ExistsByRunnerAsync(raceId, runner.Id, target.Id == 0 ? null : target.Id, ct);
        if (yaOcupado)
        {
            var existing = await resultRepository.GetByRunnerWithCategoryAsync(raceId, runner.Id, ct)
                ?? throw new NotFoundException($"Inconsistencia: dorsal '{dorsal}' marcado ocupado pero sin resultado asociado.");

            var groupId = ResolveDisputeGroupId(existing);

            // Posicion=0 en ambos: un resultado que deja de ser Valido no puede seguir
            // mostrando una posición real. `target` normalmente entra acá sin posición
            // propia todavía (recién creado, o nunca tuvo dorsal), pero el caso borde de
            // reasignar el dorsal de un resultado YA posicionado a uno que resulta
            // duplicado sí existe — resetear siempre es correcto y nunca destructivo.
            target.Estado = ResultEstado.Controversia;
            target.DorsalPropuesto = dorsal;
            target.DisputeGroupId = groupId;
            target.DisputeMotivo = Domain.Entities.DisputeMotivo.DorsalDuplicado;
            target.Posicion = 0;

            existing.Estado = ResultEstado.Controversia;
            existing.DisputeGroupId = groupId;
            existing.DisputeMotivo = Domain.Entities.DisputeMotivo.DorsalDuplicado;
            existing.Posicion = 0;

            await RegisterAuditIfChangedAsync(target.Id, actorUserId, "Estado", ResultEstado.Valido.ToString(), ResultEstado.Controversia.ToString(), "Dorsal duplicado", ct);

            if (existing.CategoryId is { } existingCategoryId)
                await RecalculatePositionsAsync(raceId, existingCategoryId, ct);

            await raceDashboardNotifier.NotifyDisputeOpenedAsync(raceId, ct);
            return true;
        }

        var association = await raceCategoryRepository.GetAssociationAsync(raceId, runner.CategoryId, ct);
        if (association is null || association.Estado != RaceCategoryStatus.EnCurso)
        {
            target.Estado = ResultEstado.Controversia;
            target.DorsalPropuesto = dorsal;
            target.DisputeMotivo = association?.Estado == RaceCategoryStatus.Terminada
                ? Domain.Entities.DisputeMotivo.CategoriaCerrada
                : Domain.Entities.DisputeMotivo.CategoriaSinSalida;
            target.Posicion = 0; // mismo motivo que en la rama de dorsal duplicado, arriba

            await RegisterAuditIfChangedAsync(target.Id, actorUserId, "Estado", ResultEstado.Valido.ToString(), ResultEstado.Controversia.ToString(), target.DisputeMotivo.ToString()!, ct);
            await raceDashboardNotifier.NotifyDisputeOpenedAsync(raceId, ct);
            return true;
        }

        target.Dorsal = runner.Dorsal;
        target.RunnerId = runner.Id;
        target.CategoryId = runner.CategoryId;
        return false;
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
        result.UpdatedAt,
        result.Estado,
        result.DorsalPropuesto,
        result.DisputeGroupId,
        result.DisputeMotivo);

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
