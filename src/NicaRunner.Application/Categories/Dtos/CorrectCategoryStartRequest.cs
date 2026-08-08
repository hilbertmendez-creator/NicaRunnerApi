using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Categories.Dtos;

/// <summary>
/// F5 del spec (motivo CategoriaSinSalida): una categoría nunca arrancó y un Admin le
/// pone la hora real después de los hechos. Distinto de `start` — ese es el disparo
/// atómico del juez de partida ("mismo balazo, mismo cero" para varias categorías a la
/// vez); esto es una corrección retroactiva de UNA categoría con SU propia hora, así
/// que no comparte el shape multi-categoría de CategoryTransitionRequest.
/// </summary>
public record CorrectCategoryStartRequest(
    [Required] DateTime StartUtc,
    [Required, MinLength(3), MaxLength(300)] string Razon);
