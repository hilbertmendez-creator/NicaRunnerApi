using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Categories.Dtos;

/// <summary>
/// Salida en falso: el balazo se dio, pero la carrera no arrancó de verdad y hay que
/// devolver las categorías afectadas a Planeada como si nunca hubieran salido.
///
/// Es la operación INVERSA de <see cref="CategoryTransitionRequest"/> en `start`, y no
/// comparte su shape a propósito: `start` lleva los tres campos del reloj de cliente
/// porque su trabajo es fijar un cero; esto BORRA el cero y no tiene ninguno que fijar.
/// Lo que sí exige, y `start` no, es una razón — un cero borrado con llegadas anuladas
/// detrás es la clase de cambio que alguien va a tener que explicar en la premiación.
/// </summary>
public record ResetCategoryStartRequest(
    [Required, MinLength(1)] List<int> CategoryIds,
    [Required, MinLength(3), MaxLength(300)] string Razon);
