using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Runners;
using NicaRunner.Application.Runners.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[Route("api/races/{raceId:int}/runners")]
[Authorize]
public class RunnersController(IRunnerService runnerService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<ActionResult<RunnerDto>> Create(int raceId, CreateRunnerRequest request, CancellationToken ct)
    {
        var runner = await runnerService.CreateAsync(raceId, request, ct);
        return CreatedAtAction(nameof(GetById), new { raceId, runnerId = runner.Id }, runner);
    }

    [HttpGet]
    public async Task<ActionResult<List<RunnerDto>>> GetAll(int raceId, CancellationToken ct) =>
        Ok(await runnerService.GetAllByRaceAsync(raceId, ct));

    [HttpGet("{runnerId:int}")]
    public async Task<ActionResult<RunnerDto>> GetById(int raceId, int runnerId, CancellationToken ct) =>
        Ok(await runnerService.GetByIdAsync(raceId, runnerId, ct));

    [HttpPut("{runnerId:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Administrador)},{nameof(UserRole.Capturista)}")]
    public async Task<ActionResult<RunnerDto>> Update(int raceId, int runnerId, UpdateRunnerRequest request, CancellationToken ct) =>
        Ok(await runnerService.UpdateAsync(raceId, runnerId, request, ct));

    [HttpDelete("{runnerId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    public async Task<IActionResult> Delete(int raceId, int runnerId, CancellationToken ct)
    {
        await runnerService.DeleteAsync(raceId, runnerId, ct);
        return NoContent();
    }

    [HttpPost("/api/races/{raceId:int}/import-excel")]
    [Authorize(Roles = nameof(UserRole.Administrador))]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportRunnersResultDto>> ImportExcel(int raceId, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest("Debe adjuntar un archivo Excel (.xlsx) con al menos una fila de datos.");

        await using var stream = file.OpenReadStream();
        var result = await runnerService.ImportFromExcelAsync(raceId, stream, ct);
        return Ok(result);
    }
}
