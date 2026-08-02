using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NicaRunner.Application.PublicResults;
using NicaRunner.Application.PublicResults.Dtos;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/public")]
[Route("api/v{version:apiVersion}/public")]
[AllowAnonymous]
[EnableRateLimiting("public-results")]
public class PublicResultsController(IPublicResultService publicResultService) : ControllerBase
{
    [HttpGet("results/{token}")]
    public async Task<ActionResult<PublicRaceResultsDto>> GetResults(string token, CancellationToken ct) =>
        Ok(await publicResultService.GetResultsByTokenAsync(token, ct));

    [HttpGet("runner/{token}/{runnerId:int}")]
    public async Task<ActionResult<PublicRunnerDetailDto>> GetRunnerResult(string token, int runnerId, CancellationToken ct) =>
        Ok(await publicResultService.GetRunnerResultByTokenAsync(token, runnerId, ct));

    // design.md Decisión 7 / spec.md "Opaque Share Key Addressing": restricción de ruta
    // [A-Za-z0-9_-]{16,32} — una clave que no calza el patrón nunca llega al action,
    // ASP.NET responde 404 antes de tocar la base de datos. Doble escapado necesario en
    // el template: las llaves {{16,32}} porque ASP.NET usa {} para parámetros de ruta, Y
    // los corchetes [[ ]] porque el reemplazo de tokens [controller]/[action] de
    // attribute routing intercepta CUALQUIER '[...]' literal (incluida una clase de
    // caracteres regex) si no se escapa — sin esto el arranque falla con
    // "a replacement value for the token 'A-Za-z0-9_-' could not be found" (verificado
    // arrancando la API real). Mismo patrón probado de forma independiente en
    // PublicResultsControllerTests.
    [HttpGet("corredor/{shareKey:regex(^[[A-Za-z0-9_-]]{{16,32}}$)}")]
    public async Task<ActionResult<PublicRunnerShareDto>> GetRunnerByShareKey(string shareKey, CancellationToken ct) =>
        Ok(await publicResultService.GetRunnerByShareKeyAsync(shareKey, ct));
}
