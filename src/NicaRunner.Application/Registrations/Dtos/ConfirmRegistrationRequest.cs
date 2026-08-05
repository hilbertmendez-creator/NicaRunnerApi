using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Registrations.Dtos;

// design.md D7: Dorsal es SIEMPRE admin-supplied acá — no existe generación/probing.
// [Required] ya rechaza un confirm sin dorsal antes de tocar el servicio (registration-
// review spec.md "Confirmation rejected without a supplied dorsal").
public record ConfirmRegistrationRequest(
    [Required, MaxLength(20)] string Dorsal);
