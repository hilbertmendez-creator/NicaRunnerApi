namespace NicaRunner.Domain.Entities;

// Valido: cuenta para posiciones y podio. Controversia: dato guardado, en revisión —
// nunca se descarta el instante capturado, solo se marca dudoso hasta que un Admin lo
// resuelve. Anulado: el juez (o un Admin) deshizo la captura — nunca un borrado físico,
// la evidencia de que alguien se equivocó es parte del resultado.
public enum ResultEstado
{
    Valido,
    Controversia,
    Anulado
}
