namespace NicaRunner.Application.Registrations.Dtos;

// public-registration spec.md "Public Link Access Validation": datos de carrera +
// categoría necesarios para que el formulario público se renderice, sin exponer nada más.
public record RegistrationLinkInfoDto(
    int RaceId,
    string RaceNombre,
    DateTime FechaCarrera,
    DateTime? FechaLimiteInscripcion,
    List<RegistrationCategoryOptionDto> Categorias);
