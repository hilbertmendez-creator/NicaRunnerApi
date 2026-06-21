using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Runners.Dtos;

public record UpdateRunnerRequest(
    [Required, MaxLength(150)] string Nombre,
    [Required, MaxLength(20)] string Dorsal,
    [MaxLength(20)] string? Telefono,
    [EmailAddress] string? Email,
    [Range(0, 120)] int Edad,
    [Required] int CategoryId);
