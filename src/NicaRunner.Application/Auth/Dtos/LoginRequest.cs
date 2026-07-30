using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Auth.Dtos;

/// <summary>
/// user-auth: "Unified Identifier Login" + "Backward-Compatible Login Payload"
/// (design.md §3.1/§4.1). <see cref="Identifier"/> es el campo nuevo (acepta email o
/// alias); <see cref="Email"/> se mantiene solo por compatibilidad con clientes ya
/// publicados que todavía envían <c>{ "email": ..., "password": ... }</c> sin
/// <c>identifier</c>. Ninguno de los dos lleva [EmailAddress]: un alias no es un email
/// válido y el login unificado no debe rechazarlo antes de llegar a AuthService.
/// </summary>
public record LoginRequest(
    [MaxLength(254)] string? Identifier,
    [MaxLength(254)] string? Email,
    [Required] string Password) : IValidatableObject
{
    /// <summary>Identificador efectivo: prioriza el campo nuevo, cae al legado.</summary>
    public string EffectiveIdentifier => Identifier ?? Email ?? string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Identifier) && string.IsNullOrWhiteSpace(Email))
            yield return new ValidationResult(
                "Debe enviar 'identifier' (o, para compatibilidad, 'email').",
                [nameof(Identifier), nameof(Email)]);
    }
}
