namespace NicaRunner.Domain.Entities;

/// <summary>
/// De dónde salió el instante de llegada de un Result. Espejo deliberado de
/// <see cref="StartClockOrigen"/>: el cero y la llegada son los dos extremos de un tiempo
/// oficial, y si uno declara la confiabilidad de su reloj el otro no puede callarla.
///
/// Servidor: camino normal, el juez tenía señal y el reloj del servidor selló la llegada.
/// Cliente: el juez no tenía señal; el dispositivo selló la llegada con un reloj que había
/// calibrado contra el servidor antes de perderla. ClienteSinCalibrar: nunca hubo señal
/// para calibrar y se aceptó el reloj crudo del dispositivo, marcado fuerte en BO.
/// CorreccionManual: alguien editó TiempoLlegada a mano vía UpdateResultRequest.
/// </summary>
public enum TiempoLlegadaOrigen
{
    Servidor,
    Cliente,
    ClienteSinCalibrar,
    CorreccionManual
}
