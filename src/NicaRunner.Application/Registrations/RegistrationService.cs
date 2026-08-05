using Microsoft.Extensions.Options;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Registrations.Dtos;
using NicaRunner.Application.Runners;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Registrations;

public class RegistrationService(
    IRegistrationRepository registrationRepository,
    IRegistrationLinkRepository linkRepository,
    IRaceRepository raceRepository,
    IRaceCategoryRepository raceCategoryRepository,
    IRunnerService runnerService,
    IAuditService auditService,
    IOptions<RegistrationOptions> registrationOptions,
    IExcelRegistrationParser excelRegistrationParser) : IRegistrationService
{
    // Cota generosa para "fecha de nacimiento implausible" (spec.md "Future or
    // implausible date of birth") — más ancha que cualquier corredor real, a propósito.
    private const int MaxEdadPlausible = 130;

    public async Task<RegistrationLinkInfoDto> GetLinkInfoAsync(string token, CancellationToken ct = default)
    {
        var (race, _) = await ResolveOpenLinkAsync(token, ct);

        var categorias = await BuildCategoryOptionsAsync(race.Id, ct);

        return new RegistrationLinkInfoDto(race.Id, race.Nombre, race.FechaCarrera, race.FechaLimiteInscripcion, categorias);
    }

    public async Task<RegistrationDto> SubmitAsync(string token, SubmitRegistrationRequest request, CancellationToken ct = default)
    {
        var (race, _) = await ResolveOpenLinkAsync(token, ct);

        // spec.md "Deadline passed": el plazo se valida contra el momento de la
        // sumisión, no contra FechaCarrera — un plazo nulo significa sin límite.
        if (race.FechaLimiteInscripcion is { } deadline && DateTime.UtcNow > deadline)
            throw new ConflictException("El plazo de inscripción para esta carrera ya finalizó.");

        EnsurePlausibleBirthDateOrThrow(request.FechaNacimiento);

        var raceCategory = await raceCategoryRepository.GetRaceCategoryByIdAsync(race.Id, request.RaceCategoryId, ct)
            ?? throw new NotFoundException($"La categoría con id {request.RaceCategoryId} no existe en esta carrera.");

        var edad = EdadCalculator.AtRaceDate(request.FechaNacimiento, race.FechaCarrera);

        // design.md closing note: la sumisión valida el mismo rango edad-categoría que
        // confirm exigiría más tarde vía RunnerService, para no fallar recién al confirmar.
        if (edad < raceCategory.Category.EdadMinima || edad > raceCategory.Category.EdadMaxima)
            throw new ValidationException(
                $"La edad ({edad}) no corresponde al rango de la categoría '{raceCategory.Category.NombreCategoria}' " +
                $"({raceCategory.Category.EdadMinima}-{raceCategory.Category.EdadMaxima}).");

        var esMenor = edad < registrationOptions.Value.EdadMayoriaEdad;
        if (esMenor && (string.IsNullOrWhiteSpace(request.ContactoEmergenciaNombre) || string.IsNullOrWhiteSpace(request.ContactoEmergenciaTelefono)))
            throw new ValidationException("Los corredores menores de edad deben indicar un contacto de emergencia (nombre y teléfono).");

        var registration = new Registration
        {
            RaceId = race.Id,
            RaceCategoryId = raceCategory.Id,
            Nombre = request.Nombre.Trim(),
            Apellidos = request.Apellidos?.Trim(),
            Telefono = request.Telefono,
            Email = request.Email,
            Sexo = request.Sexo,
            Club = request.Club,
            FechaNacimiento = request.FechaNacimiento,
            ContactoEmergenciaNombre = esMenor ? request.ContactoEmergenciaNombre : null,
            ContactoEmergenciaTelefono = esMenor ? request.ContactoEmergenciaTelefono : null,
            Estado = RegistrationStatus.Pendiente,
            PrecioAplicado = raceCategory.Precio
            // Capacidad NUNCA se chequea acá — spec.md "Submission not capacity-gated".
        };

        await registrationRepository.AddAsync(registration, ct);
        await registrationRepository.SaveChangesAsync(ct);

        return ToDto(registration);
    }

    public async Task<RegistrationDto> UploadReceiptAsync(string token, int registrationId, UploadReceiptRequest request, CancellationToken ct = default)
    {
        var (race, _) = await ResolveValidLinkAsync(token, ct);

        var registration = await registrationRepository.GetByIdAsync(race.Id, registrationId, ct)
            ?? throw new NotFoundException($"No existe la inscripción con id {registrationId} en esta carrera.");

        if (registration.Estado != RegistrationStatus.Pendiente)
            throw new ConflictException("Solo se puede adjuntar el comprobante mientras la inscripción está pendiente.");

        registration.ReferenciaTransferencia = request.ReferenciaTransferencia.Trim();
        registration.Estado = RegistrationStatus.ComprobanteSubido;

        await registrationRepository.SaveChangesAsync(ct);

        return ToDto(registration);
    }

    public async Task<List<RegistrationDto>> GetAllForReviewAsync(int raceId, RegistrationStatus? estado, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var registrations = await registrationRepository.GetAllByRaceAsync(raceId, estado, ct);
        return registrations.Select(ToDto).ToList();
    }

    // design.md D2/D3, Data Flow "admin POST .../confirm": reserve-then-compensate sin
    // transacción ambiente. Paso 1 (claim) es el latch de idempotencia — dos confirms
    // concurrentes de la misma Registration solo dejan que uno gane el rows-affected.
    // Paso 2 (reserve) es el único gate de capacidad de todo el flujo (la sumisión no lo
    // chequea). Paso 3 (promote) crea el Runner + fija Registration.RunnerId + AuditLog en
    // un solo SaveChangesAsync — si falla, se revierte 2 y 1 para sesgar hacia undersell.
    public async Task<RegistrationDto> ConfirmAsync(int raceId, int registrationId, ConfirmRegistrationRequest request, int adminId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Dorsal))
            throw new ValidationException("Debe indicar un dorsal para confirmar la inscripción.");

        var claimedRows = await registrationRepository.TryClaimAsync(registrationId, adminId, DateTime.UtcNow, ct);
        if (claimedRows == 0)
        {
            var existing = await registrationRepository.GetByIdAsync(raceId, registrationId, ct);
            if (existing is null)
                throw new NotFoundException($"No existe la inscripción con id {registrationId} en esta carrera.");

            throw new ConflictException(
                $"La inscripción debe estar en estado ComprobanteSubido para confirmarse (estado actual: {existing.Estado}).");
        }

        var registration = await registrationRepository.GetByIdAsync(raceId, registrationId, ct)
            ?? throw new NotFoundException($"No existe la inscripción con id {registrationId} en esta carrera.");

        var reservedRows = await registrationRepository.TryReserveSlotAsync(registration.RaceCategoryId, ct);
        if (reservedRows == 0)
        {
            await registrationRepository.ReleaseClaimAsync(registrationId, ct);
            // CapacityExhaustedException (design.md D13): ConfirmBulkAsync catches this
            // specific type to mark the RaceCategory exhausted in-memory; it still
            // derives from ConflictException so this single-confirm caller keeps getting
            // an ordinary 409.
            throw new CapacityExhaustedException(registration.RaceCategoryId, "La categoría de esta inscripción ya alcanzó su cupo máximo.");
        }

        try
        {
            var runner = await runnerService.CreateFromRegistrationAsync(registration, request.Dorsal, ct);

            // Fixup de FK vía navegación: runner.Id todavía es 0 acá (no persistido), pero
            // EF resuelve Registration.RunnerId al hacer SaveChangesAsync — ver el
            // comentario en IRunnerService.CreateFromRegistrationAsync.
            registration.Runner = runner;

            auditService.TrackChanges(AuditEntityTypes.Registration, registration.Id, adminId,
            [
                new FieldChange("Estado", nameof(RegistrationStatus.ComprobanteSubido), nameof(RegistrationStatus.Confirmada)),
                new FieldChange("Dorsal", null, request.Dorsal)
            ]);

            // TODO(Phase 4, tasks.md 4.1/4.3): llamar a RegistrationNotifier acá una vez
            // que exista — best-effort, sin bloquear ni reintentar la confirmación.

            await registrationRepository.SaveChangesAsync(ct);
        }
        catch
        {
            await registrationRepository.ReleaseSlotAsync(registration.RaceCategoryId, ct);
            await registrationRepository.ReleaseClaimAsync(registrationId, ct);
            throw;
        }

        return ToDto(registration);
    }

    public async Task<RegistrationDto> RejectAsync(int raceId, int registrationId, RejectRegistrationRequest request, int adminId, CancellationToken ct = default)
    {
        var registration = await registrationRepository.GetByIdAsync(raceId, registrationId, ct)
            ?? throw new NotFoundException($"No existe la inscripción con id {registrationId} en esta carrera.");

        if (registration.Estado != RegistrationStatus.Pendiente && registration.Estado != RegistrationStatus.ComprobanteSubido)
            throw new ConflictException(
                $"Solo se puede rechazar una inscripción Pendiente o ComprobanteSubido (estado actual: {registration.Estado}).");

        var estadoAnterior = registration.Estado;

        registration.Estado = RegistrationStatus.Rechazada;
        registration.MotivoRechazo = request.MotivoRechazo.Trim();
        registration.RevisadoPorUserId = adminId;
        registration.RevisadoAt = DateTime.UtcNow;

        auditService.TrackChanges(AuditEntityTypes.Registration, registration.Id, adminId,
        [
            new FieldChange("Estado", estadoAnterior.ToString(), nameof(RegistrationStatus.Rechazada)),
            new FieldChange("MotivoRechazo", null, registration.MotivoRechazo)
        ]);

        // TODO(Phase 4, tasks.md 4.1/4.3): llamar a RegistrationNotifier acá una vez que
        // exista — best-effort, sin bloquear ni reintentar el rechazo.

        await registrationRepository.SaveChangesAsync(ct);

        return ToDto(registration);
    }

    // registration-review spec.md "Registration Link Administration" (tasks.md 2.12):
    // opening a link requires at least one RaceCategory of this race to have Precio
    // configured. Capacidad is deliberately excluded from this gate — design.md D1
    // defines a null Capacidad as "sin límite" (unlimited), a legitimate configured
    // state, not a missing one — so requiring it non-null here would make an
    // intentionally-unlimited race category unable to ever open registration.
    public async Task<RegistrationLinkDto> CreateLinkAsync(int raceId, CreateRegistrationLinkRequest request, int creatorId, CancellationToken ct = default)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");

        var raceCategories = await raceCategoryRepository.GetAllRaceCategoriesByRaceAsync(raceId, ct);
        if (!raceCategories.Any(rc => rc.Precio.HasValue))
            throw new ConflictException(
                "Debe configurar el precio de al menos una categoría de esta carrera antes de abrir el enlace de inscripción.");

        var link = new RegistrationLink
        {
            RaceId = raceId,
            Token = GenerateToken(),
            FechaExpiracion = DateTime.UtcNow.AddDays(request.DiasExpiracion),
            CreatedBy = creatorId
        };

        await linkRepository.AddAsync(link, ct);
        await linkRepository.SaveChangesAsync(ct);

        return ToLinkDto(link);
    }

    public async Task<List<RegistrationLinkDto>> GetAllLinksAsync(int raceId, CancellationToken ct = default)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");

        var links = await linkRepository.GetAllByRaceAsync(raceId, ct);
        return links.Select(ToLinkDto).ToList();
    }

    // registration-review spec.md "Registration Link Administration": revoking marks
    // IsExpired (mirrors PublicResultToken's field), which ResolveValidLinkAsync already
    // treats as invalid — so revocation takes effect on the very next anonymous access,
    // with no separate "revoked" flag needed.
    public async Task RevokeLinkAsync(int raceId, int linkId, CancellationToken ct = default)
    {
        var link = await linkRepository.GetByIdAsync(raceId, linkId, ct)
            ?? throw new NotFoundException($"No existe el enlace de inscripción con id {linkId} en esta carrera.");

        link.IsExpired = true;

        await linkRepository.SaveChangesAsync(ct);
    }

    // registration-review spec.md "Bulk Confirm via Excel Template" (tasks.md 3.4/3.5,
    // design.md D12): export-then-fill. Scoped to this race's ComprobanteSubido
    // registrations only — an admin who downloads after some rows already got confirmed
    // individually simply sees a smaller sheet next time.
    public async Task<byte[]> GenerateBulkConfirmTemplateAsync(int raceId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        var pendientes = await registrationRepository.GetAllByRaceAsync(raceId, RegistrationStatus.ComprobanteSubido, ct);
        var referencia = await raceCategoryRepository.GetAllByRaceAsync(raceId, ct);

        return excelRegistrationParser.GenerateTemplate(pendientes, referencia);
    }

    // registration-review spec.md "Bulk Confirm via Excel Template" (tasks.md 3.5,
    // design.md D13/Data Flow "admin POST .../confirm-bulk"): per-row loop reusing
    // ConfirmAsync's own claim -> reserve -> promote pipeline (never reimplemented here),
    // one row at a time — deliberately NOT batched into a single SaveChangesAsync like
    // RunnerService.ImportFromExcelAsync, because capacity is a per-row atomic conditional
    // UPDATE (D2) and batching would dissolve exactly the guarantee it exists to provide.
    // Capacity exhaustion is tracked per RaceCategoryId in-memory (D13) so the batch's
    // remaining rows targeting an already-exhausted category fail fast without re-hitting
    // the DB, while rows for other categories keep going. Rows already confirmed before
    // the exhaustion point are never rolled back — same partial-success contract
    // ImportFromExcelAsync already has for the runner import.
    public async Task<BulkConfirmResultDto> ConfirmBulkAsync(int raceId, Stream excelStream, int adminId, CancellationToken ct = default)
    {
        await EnsureRaceExistsAsync(raceId, ct);

        List<ParsedConfirmRow> rows;
        try
        {
            rows = excelRegistrationParser.Parse(excelStream);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Mismo boundary genérico que RunnerService.ImportFromExcelAsync cruza contra
            // ClosedXML — no documenta un tipo de excepción propio para "no es OOXML válido".
            throw new ValidationException("El archivo no es un Excel válido (.xlsx) o está dañado.");
        }

        var pendientes = await registrationRepository.GetAllByRaceAsync(raceId, RegistrationStatus.ComprobanteSubido, ct);
        var pendientesById = pendientes.ToDictionary(r => r.Id);

        var seenDorsals = new HashSet<string>();
        var exhaustedCategories = new HashSet<int>();
        var errors = new List<ConfirmRowError>();
        var confirmadas = 0;

        foreach (var row in rows)
        {
            var reasons = new List<string>();

            Registration? registration = null;
            if (row.RegistrationId is null)
                reasons.Add("RegistrationId vacío o inválido");
            else if (!pendientesById.TryGetValue(row.RegistrationId.Value, out registration))
                reasons.Add($"La inscripción {row.RegistrationId} no existe o no está en estado ComprobanteSubido en esta carrera");

            if (string.IsNullOrWhiteSpace(row.Dorsal))
                reasons.Add("Dorsal vacío");

            var normalizedDorsal = string.IsNullOrWhiteSpace(row.Dorsal) ? null : DorsalNormalizer.Normalize(row.Dorsal);
            if (reasons.Count == 0 && normalizedDorsal is not null && seenDorsals.Contains(normalizedDorsal))
                reasons.Add($"El dorsal '{row.Dorsal}' está duplicado en el archivo");

            if (reasons.Count > 0)
            {
                errors.Add(new ConfirmRowError(row.Fila, string.Join("; ", reasons)));
                continue;
            }

            seenDorsals.Add(normalizedDorsal!);

            if (exhaustedCategories.Contains(registration!.RaceCategoryId))
            {
                errors.Add(new ConfirmRowError(row.Fila, "La categoría de esta inscripción ya alcanzó su cupo máximo."));
                continue;
            }

            try
            {
                await ConfirmAsync(raceId, registration.Id, new ConfirmRegistrationRequest(row.Dorsal), adminId, ct);
                confirmadas++;
            }
            catch (CapacityExhaustedException ex)
            {
                exhaustedCategories.Add(ex.RaceCategoryId);
                errors.Add(new ConfirmRowError(row.Fila, ex.Message));
            }
            catch (Exception ex) when (ex is ConflictException or ValidationException or NotFoundException)
            {
                errors.Add(new ConfirmRowError(row.Fila, ex.Message));
            }
        }

        return new BulkConfirmResultDto(rows.Count, confirmadas, errors);
    }

    private static string GenerateToken() =>
        $"{Guid.NewGuid():N}{Guid.NewGuid():N}";

    private static RegistrationLinkDto ToLinkDto(RegistrationLink link) => new(
        link.Id,
        link.RaceId,
        link.Token,
        link.FechaExpiracion,
        link.IsExpired,
        link.CreatedAt);

    // spec.md "Public Link Access Validation": revocado/expirado nunca expone datos de la
    // carrera — ambos casos colapsan al mismo NotFoundException, sin distinguir motivo.
    private async Task<(Race Race, RegistrationLink Link)> ResolveValidLinkAsync(string token, CancellationToken ct)
    {
        var link = await linkRepository.GetByTokenAsync(token, ct)
            ?? throw new NotFoundException("El enlace de inscripción no es válido.");

        if (link.IsExpired || link.FechaExpiracion < DateTime.UtcNow)
            throw new NotFoundException("El enlace de inscripción no es válido.");

        var race = await raceRepository.GetByIdAsync(link.RaceId, ct)
            ?? throw new NotFoundException("La carrera asociada a este enlace ya no existe.");

        return (race, link);
    }

    // Además del link, exige que la carrera tenga las inscripciones abiertas — usado por
    // GetLinkInfoAsync y SubmitAsync (spec.md "Registration not open"), pero no por
    // UploadReceiptAsync (subir el comprobante de una inscripción ya creada no depende de
    // que la carrera siga aceptando sumisiones nuevas).
    private async Task<(Race Race, RegistrationLink Link)> ResolveOpenLinkAsync(string token, CancellationToken ct)
    {
        var (race, link) = await ResolveValidLinkAsync(token, ct);

        if (!race.InscripcionesAbiertas)
            throw new ConflictException("Las inscripciones para esta carrera no están abiertas.");

        return (race, link);
    }

    private async Task<List<RegistrationCategoryOptionDto>> BuildCategoryOptionsAsync(int raceId, CancellationToken ct)
    {
        var raceCategories = await raceCategoryRepository.GetAllRaceCategoriesByRaceAsync(raceId, ct);
        return raceCategories
            .Select(rc => new RegistrationCategoryOptionDto(
                rc.Id,
                rc.CategoryId,
                rc.Category.NombreCategoria,
                rc.Category.Distancia,
                rc.Precio,
                rc.Category.EdadMinima,
                rc.Category.EdadMaxima))
            .ToList();
    }

    private static void EnsurePlausibleBirthDateOrThrow(DateTime fechaNacimiento)
    {
        var now = DateTime.UtcNow;
        if (fechaNacimiento.Date > now.Date)
            throw new ValidationException("La fecha de nacimiento no puede ser futura.");

        if (fechaNacimiento < now.AddYears(-MaxEdadPlausible))
            throw new ValidationException("La fecha de nacimiento no es plausible.");
    }

    private async Task EnsureRaceExistsAsync(int raceId, CancellationToken ct)
    {
        if (await raceRepository.GetByIdAsync(raceId, ct) is null)
            throw new NotFoundException($"No existe la carrera con id {raceId}.");
    }

    private static RegistrationDto ToDto(Registration registration) => new(
        registration.Id,
        registration.RaceId,
        registration.RaceCategoryId,
        registration.Nombre,
        registration.Apellidos,
        registration.Telefono,
        registration.Email,
        registration.Sexo,
        registration.Club,
        registration.FechaNacimiento,
        registration.ContactoEmergenciaNombre,
        registration.ContactoEmergenciaTelefono,
        registration.Estado,
        registration.ReferenciaTransferencia,
        registration.PrecioAplicado,
        registration.RunnerId,
        registration.RevisadoPorUserId,
        registration.RevisadoAt,
        registration.MotivoRechazo,
        registration.CreatedAt);
}
