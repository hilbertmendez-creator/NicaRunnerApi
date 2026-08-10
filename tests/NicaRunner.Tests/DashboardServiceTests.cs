using Moq;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Dashboard;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class DashboardServiceTests
{
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRaceCategoryRepository> _categories = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultRepository> _results = new();

    private DashboardService BuildService() =>
        new(_races.Object, _categories.Object, _runners.Object, _results.Object);

    private static Category MakeCategory(int id = 1, decimal distancia = 10m) =>
        new() { Id = id, NombreCategoria = "Libre", Distancia = distancia, EdadMinima = 0, EdadMaxima = 120 };

    private void SetupRace(Race race) =>
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

    [Fact]
    public async Task GetDashboardAsync_CarreraNoIniciada_TiempoEnCursoYRitmoSonNull()
    {
        SetupRace(new Race { Id = 1, Nombre = "5K", Estado = RaceStatus.Planeada, RaceStartUtc = null, AdminId = 1 });
        _categories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var dto = await BuildService().GetDashboardAsync(1);

        Assert.Null(dto.TiempoEnCursoSegundos);
        Assert.Null(dto.RitmoPromedioSegundosPorKm);
    }

    [Fact]
    public async Task GetDashboardAsync_CarreraEnCurso_CalculaTiempoEnCursoDesdeRaceStartUtc()
    {
        var startUtc = DateTime.UtcNow.AddMinutes(-30);
        SetupRace(new Race { Id = 1, Nombre = "5K", Estado = RaceStatus.EnCurso, RaceStartUtc = startUtc, AdminId = 1 });
        _categories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var dto = await BuildService().GetDashboardAsync(1);

        Assert.NotNull(dto.TiempoEnCursoSegundos);
        Assert.InRange(dto.TiempoEnCursoSegundos!.Value, 1795, 1805); // ~30 min, tolerancia por tiempo de ejecución del test
    }

    [Fact]
    public async Task GetDashboardAsync_TerminadaConRaceStartUtc_TiempoEnCursoEsNull()
    {
        // Solo EnCurso muestra un cronómetro corriendo; Terminada ya no "está en curso".
        SetupRace(new Race { Id = 1, Nombre = "5K", Estado = RaceStatus.Terminada, RaceStartUtc = DateTime.UtcNow.AddHours(-2), AdminId = 1 });
        _categories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var dto = await BuildService().GetDashboardAsync(1);

        Assert.Null(dto.TiempoEnCursoSegundos);
    }

    [Fact]
    public async Task GetDashboardAsync_RitmoPromedio_PromediaSoloResultadosValidosConCategoria()
    {
        var startUtc = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var category = MakeCategory(id: 1, distancia: 10m); // 10 km

        SetupRace(new Race { Id = 1, Nombre = "10K", Estado = RaceStatus.EnCurso, RaceStartUtc = startUtc, AdminId = 1 });
        _categories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([category]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([
            // 50 min para 10km => 300 s/km
            new Result { Id = 1, RaceId = 1, CategoryId = 1, TiempoLlegada = startUtc.AddMinutes(50), Estado = ResultEstado.Valido, CapturistaId = 1 },
            // 60 min para 10km => 360 s/km
            new Result { Id = 2, RaceId = 1, CategoryId = 1, TiempoLlegada = startUtc.AddMinutes(60), Estado = ResultEstado.Valido, CapturistaId = 1 },
            // Anulado: no debe contar
            new Result { Id = 3, RaceId = 1, CategoryId = 1, TiempoLlegada = startUtc.AddMinutes(10), Estado = ResultEstado.Anulado, CapturistaId = 1 },
            // Sin categoría resuelta: no debe contar
            new Result { Id = 4, RaceId = 1, CategoryId = null, TiempoLlegada = startUtc.AddMinutes(20), Estado = ResultEstado.Valido, CapturistaId = 1 },
        ]);

        var dto = await BuildService().GetDashboardAsync(1);

        // Promedio de 300 y 360 = 330 s/km
        Assert.Equal(330, dto.RitmoPromedioSegundosPorKm);
    }

    [Fact]
    public async Task GetDashboardAsync_SinResultadosValidos_RitmoPromedioEsNull()
    {
        SetupRace(new Race { Id = 1, Nombre = "5K", Estado = RaceStatus.EnCurso, RaceStartUtc = DateTime.UtcNow, AdminId = 1 });
        _categories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var dto = await BuildService().GetDashboardAsync(1);

        Assert.Null(dto.RitmoPromedioSegundosPorKm);
    }
}
