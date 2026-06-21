using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface INotificationLogRepository
{
    Task<NotificationLog?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(NotificationLog log, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
