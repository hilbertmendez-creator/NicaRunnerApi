namespace NicaRunner.Application.Common.Exceptions;

/// <summary>
/// Dos POSTs concurrentes con el mismo Idempotency-Key chocaron contra la
/// constraint única (RaceId, IdempotencyKey). El segundo perdió el race —
/// el caller debe re-leer el Result existente y devolverlo en vez de fallar.
/// Lanzada desde el repositorio (que sabe de EF) para no leakear
/// DbUpdateException a la capa de Application.
/// </summary>
public class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException()
        : base("Captura concurrente con el mismo Idempotency-Key.") { }
}
