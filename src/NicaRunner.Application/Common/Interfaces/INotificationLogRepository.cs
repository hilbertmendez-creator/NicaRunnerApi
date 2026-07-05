using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface INotificationLogRepository
{
    Task<NotificationLog?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(NotificationLog log, CancellationToken ct = default);

    /// <summary>
    /// Todo lo que sigue esperando envío: Pendiente (recién creado por
    /// NotifyAllAsync, o un intento anterior que no llegó a actualizar
    /// estado) o Fallida con margen de reintento (IntentosEnvio por debajo
    /// del máximo). Usado por el barrido de ProcessPendingAsync.
    /// </summary>
    Task<List<NotificationLog>> GetPendingOrRetryableAsync(int maxIntentos, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
