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
    List<RecentResultDto> UltimosResultados,
    // null hasta que la carrera arranca (Race.RaceStartUtc). Segundos enteros: el
    // frontend decide el formato (design.md Decisión 5), nunca un TimeSpan pre-formateado.
    int? TiempoEnCursoSegundos,
    // null si no hay ningún resultado Valido con categoría y tiempo de largada.
    // Promedio de (TiempoLlegada - RaceStartUtc) / Category.Distancia sobre esos resultados.
    int? RitmoPromedioSegundosPorKm);
