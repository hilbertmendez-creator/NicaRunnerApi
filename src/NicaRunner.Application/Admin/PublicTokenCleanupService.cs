using NicaRunner.Application.Common.Interfaces;

namespace NicaRunner.Application.Admin;

public class PublicTokenCleanupService(IPublicResultTokenRepository repository) : IPublicTokenCleanupService
{
    public async Task<CleanupResult> RunAsync(CancellationToken ct = default)
    {
        var deleted = await repository.DeleteExpiredAsync(DateTime.UtcNow, ct);
        return new CleanupResult(deleted);
    }
}
