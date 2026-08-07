using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Categories.Dtos;

/// <summary>
/// Salida (o cierre) de una o varias categorías en UNA sola operación.
/// Varias categorías que salen con el mismo disparo comparten exactamente el mismo
/// StartUtc; N llamadas sueltas darían N instantes distintos separados por la latencia
/// de red, y eso no es un detalle de performance: es un dato falso.
///
/// Los tres campos de reloj de cliente son EXCLUSIVOS del camino offline (ver
/// RaceCategoryService.StartAsync). CloseAsync y ReopenAsync los ignoran — el cierre
/// es administrativo, nada se calcula a partir de él.
/// </summary>
public record CategoryTransitionRequest(
    [Required, MinLength(1)] List<int> CategoryIds,
    DateTime? StartUtcCliente = null,
    int? OffsetConfianzaMs = null,
    int? CalibradoHaceMs = null);
