using System.ComponentModel.DataAnnotations;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Races.Dtos;

public record UpdateRaceRequest(
    [Required, MaxLength(150)] string Nombre,
    string? Descripcion,
    [Required] DateTime FechaCarrera,
    [Required] RaceStatus Estado);
