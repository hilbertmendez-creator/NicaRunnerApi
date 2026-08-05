using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Registrations.Dtos;

// D6: FechaNacimiento es requerida (nunca opcional como en CreateRunnerRequest) — la
// sumisión pública siempre conoce la fecha exacta, a diferencia del dato legado que
// RunnerService todavía tolera sin ella.
public record SubmitRegistrationRequest(
    [Required, MaxLength(150)] string Nombre,
    [MaxLength(150)] string? Apellidos,
    [MaxLength(20)] string? Telefono,
    [EmailAddress] string? Email,
    Sexo? Sexo,
    [MaxLength(150)] string? Club,
    [Required] DateTime FechaNacimiento,
    [Required] int RaceCategoryId,
    [MaxLength(150)] string? ContactoEmergenciaNombre,
    [MaxLength(30)] string? ContactoEmergenciaTelefono);
