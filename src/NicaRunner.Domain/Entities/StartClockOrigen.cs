namespace NicaRunner.Domain.Entities;

// Servidor: camino normal, online. Cliente: el juez de partida no tenía señal y el
// disparo se calibró contra el reloj del servidor antes de perderla. ClienteSinCalibrar:
// nunca hubo señal para calibrar — se acepta el reloj crudo del dispositivo, marcado
// fuerte en BO. CorreccionAdmin: la categoría nunca se arrancó a tiempo (o se cerró sin
// haber arrancado) y un Admin le puso la hora real después de los hechos, vía
// RaceCategoryService.CorrectStartAsync — no es un reloj de juez, es una corrección
// administrativa retroactiva. Un cero dudoso y señalado es mejor que ningún cero.
public enum StartClockOrigen
{
    Servidor,
    Cliente,
    ClienteSinCalibrar,
    CorreccionAdmin
}
