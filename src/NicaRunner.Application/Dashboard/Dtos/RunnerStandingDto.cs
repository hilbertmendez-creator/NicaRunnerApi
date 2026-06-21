namespace NicaRunner.Application.Dashboard.Dtos;

public record RunnerStandingDto(
    int RunnerId,
    string Nombre,
    string Dorsal,
    int Posicion,
    DateTime TiempoLlegada);
