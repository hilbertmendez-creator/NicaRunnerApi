namespace NicaRunner.Application.Dashboard.Dtos;

public record CategoryStandingsDto(
    int CategoryId,
    string NombreCategoria,
    decimal Distancia,
    List<RunnerStandingDto> Resultados);
