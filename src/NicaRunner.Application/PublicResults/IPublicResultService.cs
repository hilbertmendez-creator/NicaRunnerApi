using NicaRunner.Application.PublicResults.Dtos;

namespace NicaRunner.Application.PublicResults;

public interface IPublicResultService
{
    Task<PublicTokenDto> CreateTokenAsync(int raceId, CreatePublicTokenRequest request, int creatorId, CancellationToken ct = default);
    Task<List<PublicTokenDto>> GetAllByRaceAsync(int raceId, CancellationToken ct = default);
    Task<PublicRaceResultsDto> GetResultsByTokenAsync(string token, CancellationToken ct = default);
    Task<PublicRunnerDetailDto> GetRunnerResultByTokenAsync(string token, int runnerId, CancellationToken ct = default);

    /// <summary>
    /// design.md Decisión 3/7: enlace público permanente de detalle de un corredor,
    /// resuelto por shareKey (sin token de carrera). 404 uniforme si la clave no
    /// resuelve a ningún corredor — ver spec.md "Opaque Share Key Addressing".
    /// </summary>
    Task<PublicRunnerShareDto> GetRunnerByShareKeyAsync(string shareKey, CancellationToken ct = default);
}
