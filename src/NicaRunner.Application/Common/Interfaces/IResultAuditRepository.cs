using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public interface IResultAuditRepository
{
    Task<List<ResultAudit>> GetAllByResultAsync(int resultId, CancellationToken ct = default);
    Task AddAsync(ResultAudit audit, CancellationToken ct = default);
}
