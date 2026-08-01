using NicaRunner.Application.Notifications;

namespace NicaRunner.Api.Workers;

/// <summary>
/// Barrido periódico de notificaciones en cola (Pendiente / Fallida reintentable).
/// Antes lo disparaba un cron de GitHub Actions cada 5 min, pero eso exigía
/// sincronizar ADMIN_CLEANUP_SECRET entre Render y GitHub. Correrlo in-process
/// evita esa dependencia y reduce latencia (sin round-trip HTTP externo).
/// </summary>
public sealed class PendingNotificationsWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PendingNotificationsWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Dejar que terminen migraciones/seed antes del primer barrido.
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var result = await notificationService.ProcessPendingAsync(stoppingToken);

                if (result.Procesadas > 0)
                {
                    logger.LogInformation(
                        "Barrido de notificaciones: {Procesadas} procesadas, {Enviadas} enviadas, {Fallidas} fallidas.",
                        result.Procesadas, result.Enviadas, result.Fallidas);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en el barrido de notificaciones pendientes.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
