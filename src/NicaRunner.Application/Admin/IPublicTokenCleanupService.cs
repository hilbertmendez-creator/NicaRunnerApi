namespace NicaRunner.Application.Admin;

public interface IPublicTokenCleanupService
{
    Task<CleanupResult> RunAsync(CancellationToken ct = default);
}
