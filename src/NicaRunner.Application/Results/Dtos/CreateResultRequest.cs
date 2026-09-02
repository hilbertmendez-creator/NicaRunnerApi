using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Results.Dtos;

/// <summary>
/// Captura de un tiempo. El dorsal es opcional: en field-ops el juez suele
/// registrar la llegada sin saber aún el dorsal, y lo asigna después vía
/// UpdateResultRequest. Mientras el dorsal no se asigna, el resultado no
/// tiene runner/categoría y no entra en el cálculo de posiciones.
///
/// El camino normal NO lleva tiempo de llegada: el servidor lo fija con su propio reloj
/// al recibir el request (ver ResultService.CreateAsync). El reloj de cada celular en la
/// meta no es confiable como fuente de verdad — dos jueces con el reloj desincronizado
/// producirían tiempos inconsistentes para la misma carrera.
///
/// <see cref="TiempoLlegadaCliente"/> es la EXCEPCIÓN offline, y existe por la misma razón
/// que <c>StartUtcCliente</c> en el arranque de categorías: sin señal, el reloj del
/// servidor no está disponible en el instante que importa, y sellar la llegada cuando
/// vuelve la señal no registraría cuándo cruzó el corredor sino cuándo volvió el 4G — un
/// dato falso presentado como oficial. Se acepta el instante del cliente, se valida, y se
/// marca su origen en <c>Result.TiempoOrigen</c> para que nadie confunda después un tiempo
/// de teléfono con uno de servidor.
/// </summary>
/// <param name="Dorsal">Dorsal del corredor, si el juez ya lo sabe.</param>
/// <param name="TiempoLlegadaCliente">
/// Instante de llegada medido por el dispositivo, en UTC. Solo para el camino offline;
/// omitirlo es el camino normal y deja que mande el reloj del servidor.
/// </param>
/// <param name="OffsetConfianzaMs">
/// Medio RTT de la última calibración contra el servidor. Su ausencia junto a un
/// <paramref name="TiempoLlegadaCliente"/> significa que el dispositivo nunca llegó a
/// calibrar: el tiempo se acepta igual, marcado como ClienteSinCalibrar.
/// </param>
public record CreateResultRequest(
    [MaxLength(20)] string? Dorsal,
    DateTime? TiempoLlegadaCliente = null,
    int? OffsetConfianzaMs = null);
