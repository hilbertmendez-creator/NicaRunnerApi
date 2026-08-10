using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IAdminNotificationRepository
{
    void Add(AdminNotification notification);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<List<AdminNotification>> GetRecentAsync(int limit, CancellationToken ct = default);
    Task<int> CountUnreadAsync(CancellationToken ct = default);
    Task<AdminNotification?> GetByIdAsync(int id, CancellationToken ct = default);
    Task MarkAllReadAsync(DateTime readAtUtc, CancellationToken ct = default);
}
