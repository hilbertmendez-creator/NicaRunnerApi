using Microsoft.Extensions.Logging;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Races;

public class RaceService(
    IRaceRepository raceRepository,
    IRaceCategoryRepository raceCategoryRepository,
    ICategoryRepository categoryRepository,
    IAuditService auditService,
    IRaceCategoryService raceCategoryService,
    IResultRepository resultRepository,
    IUserRepository userRepository,
    IEnumerable<INotificationSender> notificationSenders,
    IAdminNotificationService adminNotificationService,
    ILogger<RaceService> logger) : IRaceService
{
    // Sin I, O, 0, 1 para evitar confusiones visuales al teclear el código en el móvil.
    private const string JoinCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int JoinCodeLength = 6;
    private const int MaxJoinCodeAttempts = 10;

    public async Task<RaceDto> CreateAsync(CreateRaceRequest request, int adminId, CancellationToken ct = default)
    {
        var categoryIds = request.CategoryIds?.Distinct().ToList() ?? [];
        await EnsureCategoriesExistAsync(categoryIds, ct);

        var race = new Race
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            FechaCarrera = request.FechaCarrera,
            AdminId = adminId,
            JoinCode = await GenerateUniqueJoinCodeAsync(ct)
        };

        await raceRepository.AddAsync(race, ct);
        await raceRepository.SaveChangesAsync(ct);

        foreach (var categoryId in categoryIds)
            await raceCategoryRepository.SelectAsync(race.Id, categoryId, ct);

        if (categoryIds.Count > 0)
            await raceCategoryRepository.SaveChangesAsync(ct);

        return ToDto(race);
    }

    private async Task EnsureCategoriesExistAsync(List<int> categoryIds, CancellationToken ct)
    {
        foreach (var categoryId in categoryIds)
        {
            if (await categoryRepository.GetByIdAsync(categoryId, ct) is null)
                throw new NotFoundException($"No existe la categoría con id {categoryId} en el catálogo.");
        }
    }

    public async Task<PaginatedList<RaceDto>> GetAllAsync(int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        var paginated = await raceRepository.GetPaginatedAsync(limit, offset, ct);
        return new PaginatedList<RaceDto>(paginated.Items.Select(ToDto).ToList(), paginated.TotalCount);
    }

    public async Task<RaceDto> GetByIdAsync(int raceId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        return ToDto(race);
    }

    public async Task<RaceDto> UpdateAsync(int raceId, UpdateRaceRequest request, int currentUserId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);

        // Race.Estado es derivado (RaceStatusDeriver, recomputado por
        // RaceCategoryService.SyncRaceStateAsync en cada transición de categoría). Un
        // Estado CAMBIADO acá se revertiría solo en la próxima transición — en vez de
        // permitir ese time bomb silencioso, se rechaza en voz alta. Un valor sin
        // cambios sigue siendo un no-op válido: el formulario de edición del backoffice
        // no necesita omitir el campo para poder guardar Nombre/Descripcion/Fecha.
        if (request.Estado != race.Estado)
            throw new ValidationException(
                "El estado de la carrera es derivado. Usá POST /api/races/{id}/close o /reopen.");

        var changes = new List<FieldChange>
        {
            new("Nombre", race.Nombre, request.Nombre),
            new("Descripcion", AuditValue.Of(race.Descripcion), AuditValue.Of(request.Descripcion)),
            new("FechaCarrera", AuditValue.Of(race.FechaCarrera), AuditValue.Of(request.FechaCarrera)),
        };

        race.Nombre = request.Nombre;
        race.Descripcion = request.Descripcion;
        race.FechaCarrera = request.FechaCarrera;
        race.UpdatedAt = DateTime.UtcNow;

        auditService.TrackChanges(AuditEntityTypes.Race, race.Id, currentUserId, changes);

        await raceRepository.SaveChangesAsync(ct);
        return ToDto(race);
    }

    public async Task DeleteAsync(int raceId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        raceRepository.Remove(race);
        await raceRepository.SaveChangesAsync(ct);
    }

    public async Task<RaceDto> StartAsync(int raceId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);

        if (race.Estado != RaceStatus.Planeada)
            throw new ConflictException($"Solo se puede iniciar una carrera en estado Planeada (estado actual: {race.Estado}).");

        race.Estado = RaceStatus.EnCurso;
        race.RaceStartUtc = DateTime.UtcNow;
        race.UpdatedAt = DateTime.UtcNow;

        await raceRepository.SaveChangesAsync(ct);
        return ToDto(race);
    }

    public async Task<RaceDto> JoinByCodeAsync(JoinByCodeRequest request, int userId, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var race = await raceRepository.GetByJoinCodeAsync(code, ct)
            ?? throw new NotFoundException("No existe una carrera con ese código.");

        if (race.Estado == RaceStatus.Terminada)
            throw new ConflictException("La carrera ya está terminada y no acepta nuevos jueces.");

        if (race.AdminId == userId)
            return ToDto(race);

        if (!await raceRepository.IsJudgeAsync(race.Id, userId, ct))
        {
            await raceRepository.AddJudgeAsync(new RaceJudge { RaceId = race.Id, UserId = userId }, ct);
            await raceRepository.SaveChangesAsync(ct);
        }

        return ToDto(race);
    }

    public async Task<RaceDto> CloseAsync(int raceId, int actorUserId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        var associations = await raceCategoryRepository.GetAssociationsByRaceAsync(raceId, ct);

        // Los tres blockers se evalúan y se reportan juntos en UN solo 409 — un juez no
        // debería tener que resolverlos de a uno por request (design.md D3).
        var blockers = new List<string>();

        var planeadas = associations.Where(a => a.Estado == RaceCategoryStatus.Planeada).ToList();
        if (planeadas.Count > 0)
            blockers.Add(
                "estas categorías nunca arrancaron: " +
                string.Join(", ", planeadas.Select(a => a.Category.NombreCategoria)) + ".");

        // Corrección A: MissingRunner ya excluye Anulado en la consulta del repositorio
        // (ResultRepository.GetCloseBlockerCountsAsync) — una captura anulada sin dorsal
        // nunca va a recibir uno, así que no puede bloquear el cierre para siempre.
        var blockerCounts = await resultRepository.GetCloseBlockerCountsAsync(raceId, ct);
        if (blockerCounts.Disputed > 0)
            blockers.Add($"hay {blockerCounts.Disputed} llegada(s) en controversia sin resolver.");
        if (blockerCounts.MissingRunner > 0)
            blockers.Add($"hay {blockerCounts.MissingRunner} llegada(s) sin dorsal asignado.");

        if (blockers.Count > 0)
            throw new ConflictException("No se puede cerrar: " + string.Join(" ", blockers));

        // El target de la cascada es EXACTAMENTE las categorías EnCurso — nunca las
        // Planeada (ya bloqueadas arriba) ni las Terminada (ya lo están, se saltean).
        // RaceCategoryService.CloseAsync rechaza cualquier target no-EnCurso, así que
        // pasarle la lista completa de asociaciones lanzaría 409 sobre las ya cerradas.
        var targets = associations.Where(a => a.Estado == RaceCategoryStatus.EnCurso).ToList();
        if (targets.Count == 0)
            // Carrera ya Terminada: 200 idempotente, sin auditoría ni email — no hay
            // nada nuevo que cerrar (design.md D4).
            return ToDto(race);

        var actor = await userRepository.GetByIdAsync(actorUserId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {actorUserId}.");

        // Race.Estado NUNCA se escribe acá directamente — se stagea el diff (antes de
        // la cascada, con el valor actual) y la cascada de abajo lo deriva y persiste
        // en la misma transacción (design.md D2/D5).
        var changes = new List<FieldChange>
        {
            new("Estado", AuditValue.Of(race.Estado), AuditValue.Of(RaceStatus.Terminada)),
            new("CerradaPor", null, AuditValue.Of(actor.Role)),
        };
        auditService.TrackChanges(AuditEntityTypes.Race, race.Id, actorUserId, changes);

        var request = new CategoryTransitionRequest(CategoryIds: targets.Select(t => t.CategoryId).ToList());
        await raceCategoryService.CloseAsync(raceId, request, actorUserId, ct);

        if (actor.Role == UserRole.Capturista)
        {
            // Dos canales INDEPENDIENTES a propósito: la campana del backoffice no puede
            // quedar escondida detrás de un proveedor de email ausente o caído.
            await RecordAdminNotificationBestEffortAsync(race, actor, ct);
            await NotifyAdminsBestEffortAsync(race, actor, ct);
        }

        return ToDto(race);
    }

    public async Task<RaceDto> ReopenAsync(int raceId, int actorUserId, CancellationToken ct = default)
    {
        var race = await GetRaceOrThrowAsync(raceId, ct);
        var associations = await raceCategoryRepository.GetAssociationsByRaceAsync(raceId, ct);

        var targets = associations.Where(a => a.Estado == RaceCategoryStatus.Terminada).ToList();
        if (targets.Count == 0)
            // Carrera ya no Terminada: 200 idempotente, sin auditoría (design.md D4).
            return ToDto(race);

        var changes = new List<FieldChange>
        {
            new("Estado", AuditValue.Of(race.Estado), AuditValue.Of(RaceStatus.EnCurso)),
        };
        auditService.TrackChanges(AuditEntityTypes.Race, race.Id, actorUserId, changes);

        var request = new CategoryTransitionRequest(CategoryIds: targets.Select(t => t.CategoryId).ToList());
        await raceCategoryService.ReopenAsync(raceId, request, actorUserId, ct);

        return ToDto(race);
    }

    // Feed de la campana del backoffice (decisión 2: email + bitácora + notificación).
    // Best-effort igual que el email: la fila de bitácora ya persistida es el registro
    // durable del cierre. Se llama post-commit, así que el SaveChanges de acá no arrastra
    // nada de la cascada.
    private async Task RecordAdminNotificationBestEffortAsync(Race race, User closedBy, CancellationToken ct)
    {
        try
        {
            await adminNotificationService.NotifyAsync(
                AdminNotificationType.CarreraCerradaPorJuez,
                $"El juez {closedBy.Nombre} cerró la carrera \"{race.Nombre}\".",
                race.Id,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex, "Error registrando en la bandeja el cierre de la carrera {RaceId}.", race.Id);
        }
    }

    // Best-effort a propósito: ninguna excepción ni NotificationSendResult fallido de
    // acá puede tumbar el cierre — la fila de bitácora ya persistida es el registro
    // durable, el email es un plus (design.md D6).
    private async Task NotifyAdminsBestEffortAsync(Race race, User closedBy, CancellationToken ct)
    {
        try
        {
            var admins = await userRepository.GetByRoleAsync(UserRole.Administrador, ct);
            if (admins.Count == 0)
                return;

            // Dos INotificationSender están registrados (Resend + StubWhatsApp) — hay
            // que resolver el de Email explícitamente, igual que NotificationService.
            var sender = notificationSenders.FirstOrDefault(s => s.Channel == NotificationChannel.Email);
            if (sender is null)
            {
                logger.LogWarning(
                    "No hay un sender de Email configurado; no se notificó el cierre de la carrera {RaceId}.", race.Id);
                return;
            }

            var (subject, body) = RaceClosedAdminEmail.Build(race.Nombre, closedBy.Nombre);

            foreach (var admin in admins)
            {
                var result = await sender.SendAsync(admin.Email, body, subject, html: null, ct);
                if (!result.Success)
                    logger.LogWarning(
                        "No se pudo notificar el cierre de la carrera {RaceId} a {AdminEmail}: {Error}",
                        race.Id, admin.Email, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error notificando el cierre de la carrera {RaceId} a los Administradores.", race.Id);
        }
    }

    private async Task<Race> GetRaceOrThrowAsync(int raceId, CancellationToken ct) =>
        await raceRepository.GetByIdAsync(raceId, ct)
            ?? throw new NotFoundException($"No existe la carrera con id {raceId}.");

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxJoinCodeAttempts; attempt++)
        {
            var code = GenerateJoinCode();
            if (!await raceRepository.JoinCodeExistsAsync(code, ct))
                return code;
        }
        throw new InvalidOperationException("No se pudo generar un JoinCode único tras varios intentos.");
    }

    private static string GenerateJoinCode()
    {
        var chars = new char[JoinCodeLength];
        for (var i = 0; i < JoinCodeLength; i++)
            chars[i] = JoinCodeAlphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(JoinCodeAlphabet.Length)];
        return new string(chars);
    }

    private static RaceDto ToDto(Race race) => new(
        race.Id,
        race.Nombre,
        race.Descripcion,
        race.FechaCarrera,
        race.Estado,
        race.JoinCode,
        race.RaceStartUtc,
        race.AdminId,
        race.CreatedAt,
        race.UpdatedAt);
}
