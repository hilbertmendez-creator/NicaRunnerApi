using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Dashboard.Dtos;

public record RaceDashboardDto(
    int RaceId,
    string RaceName,
    RaceStatus Estado,
    int TotalInscritos,
    int TotalConTiempo,
    int TotalPendientes,
    List<CategoryProgressDto> Categorias,
    List<RecentResultDto> UltimosResultados);
