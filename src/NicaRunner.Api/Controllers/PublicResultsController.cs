using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.PublicResults;
using NicaRunner.Application.PublicResults.Dtos;

namespace NicaRunner.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/public")]
[Route("api/v{version:apiVersion}/public")]
[AllowAnonymous]
public class PublicResultsController(IPublicResultService publicResultService) : ControllerBase
{
    [HttpGet("results/{token}")]
    public async Task<ActionResult<PublicRaceResultsDto>> GetResults(string token, CancellationToken ct) =>
        Ok(await publicResultService.GetResultsByTokenAsync(token, ct));

    [HttpGet("runner/{token}/{runnerId:int}")]
    public async Task<ActionResult<PublicRunnerDetailDto>> GetRunnerResult(string token, int runnerId, CancellationToken ct) =>
        Ok(await publicResultService.GetRunnerResultByTokenAsync(token, runnerId, ct));
}
