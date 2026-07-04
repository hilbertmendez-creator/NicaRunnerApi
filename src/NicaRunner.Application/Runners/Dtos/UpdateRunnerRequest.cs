using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Runners.Dtos;

public record UpdateRunnerRequest(
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
