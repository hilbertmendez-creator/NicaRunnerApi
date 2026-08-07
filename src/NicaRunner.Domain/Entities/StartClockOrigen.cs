namespace NicaRunner.Domain.Entities;

// Servidor: camino normal, online. Cliente: el juez de partida no tenía señal y el
// disparo se calibró contra el reloj del servidor antes de perderla. ClienteSinCalibrar:
// nunca hubo señal para calibrar — se acepta el reloj crudo del dispositivo, marcado
// fuerte en BO. Un cero dudoso y señalado es mejor que ningún cero.
public enum StartClockOrigen
{
    Servidor,
    Cliente,
    ClienteSinCalibrar
}
