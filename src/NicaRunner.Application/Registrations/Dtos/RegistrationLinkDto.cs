namespace NicaRunner.Application.Registrations.Dtos;

// registration-review spec.md "Registration Link Administration" (tasks.md 2.12).
// Mirrors PublicTokenDto but also exposes IsExpired: unlike PublicResultToken, a
// RegistrationLink can be admin-revoked (IsExpired flips true) without a new expiry
// date, so callers need to see that state directly.
public record RegistrationLinkDto(
    int Id,
    int RaceId,
    string Token,
    DateTime FechaExpiracion,
    bool IsExpired,
    DateTime CreatedAt);
