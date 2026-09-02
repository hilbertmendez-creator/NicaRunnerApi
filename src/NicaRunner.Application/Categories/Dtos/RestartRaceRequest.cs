using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Categories.Dtos;

/// <summary>
/// Reinicio de la carrera completa por salida en falso. No lleva categorías: el alcance
/// ES la carrera entera, y hacerlo explícito con una lista solo abriría la puerta a que
/// alguien mande una lista parcial creyendo que reinicia todo. Para el reinicio parcial
/// existe <see cref="ResetCategoryStartRequest"/>, que nombra sus categorías una por una.
/// </summary>
public record RestartRaceRequest(
    [Required, MinLength(3), MaxLength(300)] string Razon);
