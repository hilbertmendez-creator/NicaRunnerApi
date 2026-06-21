namespace NicaRunner.Application.Categories.Dtos;

public record RaceCategoryDto(
    int Id,
    int RaceId,
    string NombreCategoria,
    decimal Distancia,
    int EdadMinima,
    int EdadMaxima,
    int Orden);
