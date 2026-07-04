namespace NicaRunner.Application.Categories.Dtos;

public record CategoryDto(
    int Id,
    string Codigo,
    string NombreCategoria,
    string? Descripcion,
    decimal Distancia,
    int EdadMinima,
    int EdadMaxima,
    int Orden);
