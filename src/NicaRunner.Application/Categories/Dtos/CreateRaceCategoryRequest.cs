using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Categories.Dtos;

public record CreateRaceCategoryRequest(
    [Required, MaxLength(100)] string NombreCategoria,
    [Range(0.1, 1000)] decimal Distancia,
    [Range(0, 120)] int EdadMinima,
    [Range(0, 120)] int EdadMaxima,
    int Orden);
