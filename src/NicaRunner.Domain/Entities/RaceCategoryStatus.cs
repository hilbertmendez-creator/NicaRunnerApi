namespace NicaRunner.Domain.Entities;

// Espejo de RaceStatus, pero por categoría: cada categoría arranca cuando el juez
// de partida da su orden de salida, no cuando arranca "la carrera".
public enum RaceCategoryStatus
{
    Planeada,
    EnCurso,
    Terminada
}
