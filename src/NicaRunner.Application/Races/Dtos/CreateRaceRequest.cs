using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Races.Dtos;

public record CreateRaceRequest(
    [Required, MaxLength(150)] string Nombre,
    string? Descripcion,
    [Required] DateTime FechaCarrera);
