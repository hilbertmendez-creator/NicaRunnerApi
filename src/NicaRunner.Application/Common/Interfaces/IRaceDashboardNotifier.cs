namespace NicaRunner.Application.Common.Interfaces;

/// <summary>
/// Notifica a los clientes conectados (dashboard en vivo) que los resultados de una
/// carrera cambiaron, para que dejen de depender solo del polling. La implementación
/// concreta (SignalR) vive en Api porque Application no debe depender de ASP.NET Core.
/// </summary>
public interface IRaceDashboardNotifier
{
    Task NotifyResultsChangedAsync(int raceId, CancellationToken ct = default);
    Task NotifyDisputeOpenedAsync(int raceId, CancellationToken ct = default);
}
