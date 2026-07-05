namespace NicaRunner.Application.Common.Exceptions;

/// <summary>
/// Dos capturas concurrentes del mismo corredor (misma carrera) chocaron
/// contra la constraint única (RaceId, RunnerId). El segundo POST perdió el
/// race — a diferencia de un conflicto de Idempotency-Key, este SÍ es un
/// conflicto real: el caller debe rechazarlo, no devolver el resultado ajeno.
/// Lanzada desde el repositorio (que sabe de EF) para no leakear
/// DbUpdateException a la capa de Application.
/// </summary>
public class RunnerResultConflictException : Exception
{
    public RunnerResultConflictException()
        : base("Captura concurrente del mismo corredor en esta carrera.") { }
}
