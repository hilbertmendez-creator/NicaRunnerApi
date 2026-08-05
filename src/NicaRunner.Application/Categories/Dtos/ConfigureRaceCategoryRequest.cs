using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Categories.Dtos;

// registration-review spec.md "RaceCategory Capacity and Price Configuration": sets
// Capacidad/Precio on an already-assigned RaceCategory. Capacidad stays nullable —
// design.md D1 defines null as "sin límite" (unlimited), a legitimate configured state,
// not a missing one. Precio is required: it is the field that actually gates opening a
// RegistrationLink (see RegistrationService.CreateLinkAsync).
public record ConfigureRaceCategoryRequest(
    [Range(0, int.MaxValue)] int? Capacidad,
    [Range(0, 1_000_000)] decimal Precio);
