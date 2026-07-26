using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Results.Dtos;

/// <summary>
/// Captura de un tiempo. El dorsal es opcional: en field-ops el juez suele
/// registrar la llegada sin saber aún el dorsal, y lo asigna después vía
/// UpdateResultRequest. Mientras el dorsal no se asigna, el resultado no
/// tiene runner/categoría y no entra en el cálculo de posiciones.
///
/// No lleva un tiempo de llegada: el servidor lo fija con su propio reloj al
/// recibir el request (ver ResultService.CreateAsync). El reloj de cada
/// celular en la meta no es confiable como fuente de verdad — dos jueces con
/// el reloj desincronizado producirían tiempos inconsistentes para la misma
/// carrera.
/// </summary>
public record CreateResultRequest([MaxLength(20)] string? Dorsal);
