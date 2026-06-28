using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Results.Dtos;

/// <summary>
/// Captura de un tiempo. El dorsal es opcional: en field-ops el juez suele
/// registrar la llegada sin saber aún el dorsal, y lo asigna después vía
/// UpdateResultRequest. Mientras el dorsal no se asigna, el resultado no
/// tiene runner/categoría y no entra en el cálculo de posiciones.
/// </summary>
public record CreateResultRequest(
    [MaxLength(20)] string? Dorsal,
    [Required] DateTime TiempoLlegada);
