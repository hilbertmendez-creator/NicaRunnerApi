using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class NotificationLogRepository(NicaRunnerDbContext context) : INotificationLogRepository
{
    public Task<NotificationLog?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.NotificationLogs.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task AddAsync(NotificationLog log, CancellationToken ct = default) =>
        await context.NotificationLogs.AddAsync(log, ct);

    public Task<List<NotificationLog>> GetPendingOrRetryableAsync(int maxIntentos, CancellationToken ct = default) =>
        context.NotificationLogs
            .Include(n => n.Runner)
            .Where(n => n.Status == NotificationStatus.Pendiente
                     || (n.Status == NotificationStatus.Fallida && n.IntentosEnvio < maxIntentos))
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
