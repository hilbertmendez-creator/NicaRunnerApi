namespace NicaRunner.Application.Results.Dtos;

public record ResultDto(
    int Id,
    int RaceId,
    int RunnerId,
    string Dorsal,
    DateTime TiempoLlegada,
    int Posicion,
    int CategoryId,
    int CapturistaId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
