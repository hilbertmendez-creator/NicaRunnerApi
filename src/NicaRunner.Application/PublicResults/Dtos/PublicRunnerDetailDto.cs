namespace NicaRunner.Application.PublicResults.Dtos;

public record PublicRunnerDetailDto(
    string RaceName,
    string NombreCategoria,
    decimal Distancia,
    int RunnerId,
    string Nombre,
    string Dorsal,
    int Posicion,
    DateTime TiempoLlegada);
