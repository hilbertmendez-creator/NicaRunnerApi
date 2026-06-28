namespace NicaRunner.Application.Results.Dtos;

public record ResultDto(
    int Id,
    int RaceId,
    int RunnerId,
    string RunnerNombre,
    string Dorsal,
    DateTime TiempoLlegada,
    int Posicion,
    int CategoryId,
    string CategoriaNombre,
    int CapturistaId,
    string CapturistaNombre,
    DateTime CreatedAt,
    DateTime UpdatedAt);
