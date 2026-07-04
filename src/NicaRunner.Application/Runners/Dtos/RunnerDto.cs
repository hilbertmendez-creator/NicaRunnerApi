using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Runners.Dtos;

public record RunnerDto(
    int Id,
    int RaceId,
    string Nombre,
    string? Apellidos,
    string Dorsal,
    string? Telefono,
    string? Email,
    Sexo? Sexo,
    string? Club,
    DateTime? FechaNacimiento,
    int Edad,
    int CategoryId,
    string CategoriaNombre,
    decimal Distancia,
    DateTime CreatedAt);
