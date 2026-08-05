namespace NicaRunner.Application.Registrations.Dtos;

// Opción de categoría ofrecida en el formulario público — RaceCategoryId es la clave que
// SubmitRegistrationRequest.RaceCategoryId espera de vuelta (Registration.RaceCategoryId
// referencia la fila RaceCategory, no el catálogo Category).
public record RegistrationCategoryOptionDto(
    int RaceCategoryId,
    int CategoryId,
    string NombreCategoria,
    decimal Distancia,
    decimal? Precio,
    int EdadMinima,
    int EdadMaxima);
