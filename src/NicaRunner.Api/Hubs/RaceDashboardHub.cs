using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Hubs;

/// <summary>
/// Hub de actualizaciones en vivo del dashboard. Los clientes se unen a un grupo por
/// carrera (JoinRace) para recibir "resultsChanged" cuando ResultService guarda una
/// captura o edición — reemplaza la dependencia exclusiva del polling de 5s.
/// Restringido a Admin/Lector — es el mismo dashboard de la sección 6 del spec
/// ("Dashboard (Admin & Lector)"), Capturista no lo usa para capturar.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Lector)}")]
public class RaceDashboardHub : Hub
{
    public static string GroupName(int raceId) => $"race-{raceId}";

    public Task JoinRace(int raceId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(raceId));

    public Task LeaveRace(int raceId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(raceId));
}
