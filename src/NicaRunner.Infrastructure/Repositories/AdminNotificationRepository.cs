using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class AdminNotificationRepository(NicaRunnerDbContext context) : IAdminNotificationRepository
{
    public void Add(AdminNotification notification) => context.AdminNotifications.Add(notification);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);

    public async Task<List<AdminNotification>> GetRecentAsync(int limit, CancellationToken ct = default) =>
        await context.AdminNotifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);

    public async Task<int> CountUnreadAsync(CancellationToken ct = default) =>
        await context.AdminNotifications
            .AsNoTracking()
            .CountAsync(n => n.ReadAt == null, ct);

    public async Task<AdminNotification?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await context.AdminNotifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task MarkAllReadAsync(DateTime readAtUtc, CancellationToken ct = default) =>
        await context.AdminNotifications
            .Where(n => n.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.ReadAt, readAtUtc), ct);
}
