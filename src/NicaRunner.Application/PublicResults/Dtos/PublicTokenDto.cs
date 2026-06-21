namespace NicaRunner.Application.PublicResults.Dtos;

public record PublicTokenDto(
    int Id,
    int RaceId,
    string Token,
    DateTime FechaExpiracion,
    DateTime CreatedAt);
