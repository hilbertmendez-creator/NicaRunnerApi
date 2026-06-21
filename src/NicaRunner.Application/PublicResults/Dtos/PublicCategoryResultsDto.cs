namespace NicaRunner.Application.PublicResults.Dtos;

public record PublicCategoryResultsDto(
    string NombreCategoria,
    decimal Distancia,
    List<PublicRunnerResultDto> Resultados);
