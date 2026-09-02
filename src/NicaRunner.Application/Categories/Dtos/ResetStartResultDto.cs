namespace NicaRunner.Application.Categories.Dtos;

/// <summary>
/// Qué pasó realmente en un reinicio por salida en falso.
///
/// Devolver solo las categorías dejaría invisible la mitad destructiva de la operación:
/// las llegadas que se anularon. Quien reinicia tiene que poder decir en voz alta
/// "se borraron 14 llegadas" sin ir a buscarlo a otra pantalla.
/// </summary>
/// <param name="Categories">Las categorías afectadas, ya de vuelta en Planeada.</param>
/// <param name="LlegadasAnuladas">Cuántas capturas vivas se anularon en el reinicio.</param>
public record ResetStartResultDto(
    List<RaceCategoryDto> Categories,
    int LlegadasAnuladas);
