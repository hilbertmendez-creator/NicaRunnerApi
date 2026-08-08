using Microsoft.AspNetCore.SignalR;
using NicaRunner.Application.Common.Interfaces;

namespace NicaRunner.Api.Hubs;

public class RaceDashboardNotifier(IHubContext<RaceDashboardHub> hubContext) : IRaceDashboardNotifier
{
    public Task NotifyResultsChangedAsync(int raceId, CancellationToken ct = default) =>
        hubContext.Clients.Group(RaceDashboardHub.GroupName(raceId))
            .SendAsync("resultsChanged", raceId, cancellationToken: ct);

    public Task NotifyDisputeOpenedAsync(int raceId, CancellationToken ct = default) =>
        hubContext.Clients.Group(RaceDashboardHub.GroupName(raceId))
            .SendAsync("disputeOpened", raceId, cancellationToken: ct);
}
