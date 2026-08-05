namespace NicaRunner.Application.ReservedDorsals.Dtos;

public record ReservedDorsalDto(
    int Id,
    int RaceId,
    string Dorsal,
    string? Motivo,
    int CreatedBy,
    DateTime CreatedAt);
