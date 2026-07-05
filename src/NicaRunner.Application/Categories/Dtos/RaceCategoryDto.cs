namespace NicaRunner.Application.Categories.Dtos;

public record RaceCategoryDto(
    int CategoryId,
    string Codigo,
    string NombreCategoria,
    string? Descripcion,
    decimal Distancia,
    int EdadMinima,
    int EdadMaxima,
    int Orden);
