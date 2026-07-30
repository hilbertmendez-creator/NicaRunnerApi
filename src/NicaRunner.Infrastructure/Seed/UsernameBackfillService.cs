using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;

namespace NicaRunner.Infrastructure.Seed;

/// <summary>
/// M2 (design.md §3.2): backfill de <c>Username</c> para las filas creadas antes de
/// esta migración. Código de aplicación, no SQL — el generador es C# con normalización
/// Unicode que ninguna expresión SQL portable puede reproducir igual. Idempotente y
/// seguro de re-ejecutar en cada deploy junto a <see cref="AdminUserSeeder"/>: solo toca
/// filas con <c>Username</c> nulo.
/// </summary>
public static class UsernameBackfillService
{
    // Solo controla la cadencia de flush a la base — el sondeo de unicidad de cada
    // AssignAsync sigue viendo la fila recién asignada dentro del mismo lote solo
    // después del flush. Colisión intra-lote no cerrada por el flush es un caso extremo
    // y de baja probabilidad para nombres reales; el backfill es idempotente y re-ejecuta
    // en el próximo deploy sobre cualquier fila que haya quedado con Username nulo.
    private const int BatchSize = 50;

    public static async Task BackfillAsync(IUserRepository userRepository, AliasAssigner aliasAssigner, CancellationToken ct = default)
    {
        var pending = (await userRepository.GetAllAsync(ct))
            .Where(u => u.Username is null)
            .OrderBy(u => u.Id)
            .ToList();

        var sinceLastFlush = 0;
        foreach (var user in pending)
        {
            await aliasAssigner.AssignAsync(user, ct);
            sinceLastFlush++;

            if (sinceLastFlush >= BatchSize)
            {
                await userRepository.SaveChangesAsync(ct);
                sinceLastFlush = 0;
            }
        }

        if (sinceLastFlush > 0)
            await userRepository.SaveChangesAsync(ct);
    }
}
