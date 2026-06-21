namespace NicaRunner.Application.Runners.Dtos;

public record RunnerDto(
    int Id,
    int RaceId,
    string Nombre,
    string Dorsal,
    string? Telefono,
    string? Email,
    int Edad,
    int CategoryId,
    DateTime CreatedAt);
