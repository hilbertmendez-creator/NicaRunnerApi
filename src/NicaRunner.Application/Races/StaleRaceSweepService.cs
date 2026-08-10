using Microsoft.Extensions.Logging;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races.Dtos;

namespace NicaRunner.Application.Races;

public class StaleRaceSweepService(
    IRaceRepository raceRepository,
    IResultRepository resultRepository,
    IUserRepository userRepository,
    IEnumerable<INotificationSender> notificationSenders,
    ILogger<StaleRaceSweepService> logger) : IStaleRaceSweepService
{
    // TODO(stale-race-sweep RED step): implementación real pendiente.
    public Task<StaleRaceSweepResult> RunAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();
}
