using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Auth;

/// <summary>
/// race-judge-guard: un Capturista sólo opera las carreras a las que se unió con el
/// código (RaceJudge) o que administra (AdminId). Antes el JoinCode se pedía pero nunca
/// se revisaba: cualquier Capturista podía capturar, arrancar o cerrar cualquier
/// carrera, y leer los datos personales de sus corredores.
/// </summary>
/// <remarks>
/// Lee el <c>raceId</c> de la ruta, así que sólo sirve en endpoints que lo llevan. Los
/// servicios ya buscan llegadas y corredores DENTRO de esa carrera
/// (<c>GetByIdAsync(raceId, id)</c>), de modo que revisar la carrera de la ruta alcanza:
/// cambiar el id de la llegada no salta a otra carrera.
///
/// Administrador y Lector pasan sin consulta: el primero opera todo, y lo que el
/// segundo puede hacer ya lo acota su <c>[Authorize(Roles)]</c>. El rechazo es una
/// <see cref="ForbiddenException"/> para que ExceptionHandlingMiddleware lo responda
/// como cualquier otro 403.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RaceJudgeRequiredAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (!user.IsInRole(nameof(UserRole.Capturista)))
        {
            await next();
            return;
        }

        // Un endpoint marcado sin raceId en la ruta es un error de quien lo marcó; se
        // rechaza en vez de dejarlo pasar sin revisar.
        if (!int.TryParse(context.RouteData.Values["raceId"]?.ToString(), out var raceId) ||
            !int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new ForbiddenException("No tenés acceso a esta carrera.");

        var races = context.HttpContext.RequestServices.GetRequiredService<IRaceRepository>();
        if (!await races.IsAdminOrJudgeAsync(raceId, userId, context.HttpContext.RequestAborted))
            throw new ForbiddenException(
                "No sos juez de esta carrera. Unite con el código de la carrera para operarla.");

        await next();
    }
}
