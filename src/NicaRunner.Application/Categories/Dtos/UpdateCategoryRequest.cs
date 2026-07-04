using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Categories.Dtos;

public record UpdateCategoryRequest(
    [Required, RegularExpression("^[A-Za-z0-9]{2,10}$", ErrorMessage = "El código debe ser alfanumérico (2 a 10 caracteres).")]
    string Codigo,
    [Required, MaxLength(100)] string NombreCategoria,
    [MaxLength(300)] string? Descripcion,
    [Range(0.1, 1000)] decimal Distancia,
    [Range(0, 120)] int EdadMinima,
    [Range(0, 120)] int EdadMaxima,
    int Orden);
