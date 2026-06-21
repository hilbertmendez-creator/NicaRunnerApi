using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class ResultAuditRepository(NicaRunnerDbContext context) : IResultAuditRepository
{
    public Task<List<ResultAudit>> GetAllByResultAsync(int resultId, CancellationToken ct = default) =>
        context.ResultAudits
            .Where(a => a.ResultId == resultId)
            .ToListAsync(ct);

    public async Task AddAsync(ResultAudit audit, CancellationToken ct = default) =>
        await context.ResultAudits.AddAsync(audit, ct);
}
