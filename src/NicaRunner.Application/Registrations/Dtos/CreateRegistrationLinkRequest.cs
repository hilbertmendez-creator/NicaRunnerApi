using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Registrations.Dtos;

// registration-review spec.md "Registration Link Administration" (tasks.md 2.12);
// mirrors CreatePublicTokenRequest.
public record CreateRegistrationLinkRequest(
    [Range(1, 365)] int DiasExpiracion = 30);
