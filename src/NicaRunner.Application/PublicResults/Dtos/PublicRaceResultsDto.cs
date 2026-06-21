namespace NicaRunner.Application.PublicResults.Dtos;

public record PublicRaceResultsDto(
    string RaceName,
    DateTime FechaCarrera,
    List<PublicCategoryResultsDto> Categorias);
