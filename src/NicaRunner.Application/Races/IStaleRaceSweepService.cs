using NicaRunner.Application.Races.Dtos;

namespace NicaRunner.Application.Races;

public interface IStaleRaceSweepService
{
    /// <summary>
    /// Guardia anti-zombie: detecta carreras EnCurso sin actividad de captura hace
    /// StaleThreshold o más, notifica por email a todos los Administradores, y
    /// devuelve la lista completa para el log del cron/diagnóstico. NUNCA cierra ni
    /// muta ninguna carrera — solo alerta, un humano decide qué hacer.
    /// </summary>
    Task<StaleRaceSweepResult> RunAsync(CancellationToken ct = default);
}
