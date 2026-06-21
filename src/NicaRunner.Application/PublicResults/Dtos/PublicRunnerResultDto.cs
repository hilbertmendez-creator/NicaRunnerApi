namespace NicaRunner.Application.PublicResults.Dtos;

public record PublicRunnerResultDto(
    int RunnerId,
    string Nombre,
    string Dorsal,
    int Posicion,
    DateTime TiempoLlegada);
