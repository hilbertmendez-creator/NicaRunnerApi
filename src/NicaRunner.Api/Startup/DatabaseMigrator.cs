using Microsoft.EntityFrameworkCore;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Api.Startup;

/// <summary>
/// Serializa la aplicación de migraciones entre instancias concurrentes
/// (rolling deploy) con un advisory lock de Postgres. La instancia que no
/// consigue el lock espera; cuando lo obtiene, su propio MigrateAsync() ya no
/// tiene nada pendiente y es un no-op.
/// </summary>
public static class DatabaseMigrator
{
    public static async Task MigrateWithAdvisoryLockAsync(NicaRunnerDbContext db)
    {
        const long migrationLockId = 72710051; // arbitrario, estable, exclusivo de este propósito

        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using (var lockCmd = connection.CreateCommand())
            {
                lockCmd.CommandText = $"SELECT pg_advisory_lock({migrationLockId})";
                await lockCmd.ExecuteNonQueryAsync();
            }

            await db.Database.MigrateAsync();
        }
        finally
        {
            await using (var unlockCmd = connection.CreateCommand())
            {
                unlockCmd.CommandText = $"SELECT pg_advisory_unlock({migrationLockId})";
                await unlockCmd.ExecuteNonQueryAsync();
            }

            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
