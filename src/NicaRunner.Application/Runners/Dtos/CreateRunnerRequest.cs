using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Runners.Dtos;

// Edad es opcional: si se envía FechaNacimiento, la edad se calcula a partir de
// ella (respecto a la fecha de la carrera) y el valor de Edad se ignora. Edad solo
// es obligatoria cuando no hay fecha de nacimiento (dato legado).
public record CreateRunnerRequest(
    [Required, MaxLength(150)] string Nombre,
    [MaxLength(150)] string? Apellidos,
    [Required, MaxLength(20)] string Dorsal,
    [MaxLength(20)] string? Telefono,
    [EmailAddress] string? Email,
    Sexo? Sexo,
    [MaxLength(150)] string? Club,
    DateTime? FechaNacimiento,
    [Range(0, 120)] int? Edad,
    [Required] int CategoryId);
