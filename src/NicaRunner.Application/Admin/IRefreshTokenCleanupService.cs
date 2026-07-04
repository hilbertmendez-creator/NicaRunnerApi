namespace NicaRunner.Application.Admin;

public record CleanupResult(int Deleted);

public interface IRefreshTokenCleanupService
{
    Task<CleanupResult> RunAsync(CancellationToken ct = default);
}
