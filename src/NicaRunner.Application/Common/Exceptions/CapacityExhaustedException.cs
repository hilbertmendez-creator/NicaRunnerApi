namespace NicaRunner.Application.Common.Exceptions;

/// <summary>
/// public-runner-registration-manual-payment (design.md D13): TryReserveSlotAsync
/// returned rows=0 — this Registration's RaceCategory already reached its configured
/// Capacidad. Distinguishing this from a generic ConflictException lets
/// RegistrationService.ConfirmBulkAsync mark the whole RaceCategory exhausted in-memory
/// and fail-fast the batch's remaining rows for it without re-hitting the DB, while every
/// other caller — including the single-confirm endpoint — still sees an ordinary
/// ConflictException/409, since this type derives from it and
/// ExceptionHandlingMiddleware's existing `catch (ConflictException ex)` applies
/// unchanged.
/// </summary>
public class CapacityExhaustedException(int raceCategoryId, string message) : ConflictException(message)
{
    public int RaceCategoryId { get; } = raceCategoryId;
}
