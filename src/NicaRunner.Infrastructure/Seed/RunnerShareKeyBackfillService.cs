using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;

namespace NicaRunner.Infrastructure.Seed;

/// <summary>
/// design.md Decisión 2: backfill de <c>Runner.PublicShareKey</c> para las filas creadas
/// antes de esta migración (y para cualquier fila que una instancia vieja haya insertado
/// con la clave nula durante una ventana de deploy). Código de aplicación, no SQL — el
/// generador es CSPRNG en C#; <c>random()</c> de Postgres NO lo es. Idempotente y seguro
/// de re-ejecutar en cada boot junto a <see cref="UsernameBackfillService"/>: solo toca
/// filas con <c>PublicShareKey</c> nulo.
/// </summary>
public static class RunnerShareKeyBackfillService
{
    // Misma cadencia de flush que UsernameBackfillService — solo controla cuándo se
    // persiste el lote, no afecta la unicidad (128 bits de entropía por clave hacen la
    // colisión intra-lote negligible).
    private const int BatchSize = 50;

    public static async Task BackfillAsync(IRunnerRepository runnerRepository, CancellationToken ct = default)
    {
        var pending = await runnerRepository.GetAllWithoutShareKeyAsync(ct);

        var sinceLastFlush = 0;
        foreach (var runner in pending)
        {
            runner.PublicShareKey = ShareKeyGenerator.Generate();
            sinceLastFlush++;

            if (sinceLastFlush >= BatchSize)
            {
                await runnerRepository.SaveChangesAsync(ct);
                sinceLastFlush = 0;
            }
        }

        if (sinceLastFlush > 0)
            await runnerRepository.SaveChangesAsync(ct);
    }
}
