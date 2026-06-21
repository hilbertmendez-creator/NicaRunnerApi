using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Races.Dtos;

public record RaceDto(
    int Id,
    string Nombre,
    string? Descripcion,
    DateTime FechaCarrera,
    RaceStatus Estado,
    int AdminId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
