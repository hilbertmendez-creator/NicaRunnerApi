using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Common.Dtos;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class ResultPositionExclusionTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _notifier = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<IAdminNotificationService> _adminNotifications = new();

    public ResultPositionExclusionTests()
    {
        // Default sin arrancar ninguna categoría: estos tests no ejercitan ElapsedMillis,
        // solo necesitan que ToDto no reviente por un lookup null (Task 5).
        _raceCategories.Setup(rc => rc.GetAssociationsByRaceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RaceCategory>());
    }

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object, _notifier.Object, _raceCategories.Object, _adminNotifications.Object);

    private static Result Valid(int id, DateTime tiempo) =>
        new() { Id = id, RaceId = 1, CategoryId = 5, Dorsal = $"{id}", TiempoLlegada = tiempo, Estado = ResultEstado.Valido };

    [Fact]
    public async Task RecalculatePositions_ExcluyeAnulados()
    {
        var valido = Valid(1, new DateTime(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc));
        var anulado = Valid(2, new DateTime(2026, 8, 5, 9, 59, 0, DateTimeKind.Utc));
        anulado.Estado = ResultEstado.Anulado;
        _results.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(valido);
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([valido, anulado]);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "C", JoinCode = "X", Estado = RaceStatus.EnCurso, RaceStartUtc = new DateTime(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc) });
        _runners.Setup(r => r.GetByDorsalAsync(1, "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Runner { Id = 1, RaceId = 1, Dorsal = "1", CategoryId = 5 });
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RaceCategory
            {
                RaceId = 1, CategoryId = 5, Estado = RaceCategoryStatus.EnCurso,
                Category = new Category { Id = 5, NombreCategoria = "Test" }
            });

        await BuildService().UpdateAsync(1, 1, new UpdateResultRequest("1", valido.TiempoLlegada, "ajuste"), 42);

        Assert.Equal(1, valido.Posicion);
        Assert.Equal(0, anulado.Posicion); // nunca tocado — no forma parte del cálculo
    }

    [Fact]
    public async Task RecalculatePositions_ExcluyeEnControversia()
    {
        var valido = Valid(1, new DateTime(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc));
        var enControversia = Valid(2, new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc));
        enControversia.Estado = ResultEstado.Controversia;
        _results.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(valido);
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([valido, enControversia]);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "C", JoinCode = "X", Estado = RaceStatus.EnCurso, RaceStartUtc = new DateTime(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc) });
        _runners.Setup(r => r.GetByDorsalAsync(1, "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Runner { Id = 1, RaceId = 1, Dorsal = "1", CategoryId = 5 });
        _raceCategories.Setup(rc => rc.GetAssociationAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RaceCategory
            {
                RaceId = 1, CategoryId = 5, Estado = RaceCategoryStatus.EnCurso,
                Category = new Category { Id = 5, NombreCategoria = "Test" }
            });

        await BuildService().UpdateAsync(1, 1, new UpdateResultRequest("1", valido.TiempoLlegada, "ajuste"), 42);

        Assert.Equal(1, valido.Posicion);
        Assert.Equal(0, enControversia.Posicion);
    }
}
