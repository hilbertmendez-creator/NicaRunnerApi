using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class DisputeResolutionTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _notifier = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<IAdminNotificationService> _adminNotifications = new();

    private const int AdminId = 1;

    public DisputeResolutionTests()
    {
        // ResolveDisputeAsync/GetOpenDisputesAsync enhebran el lookup de StartUtc por
        // categoría (Task 5) para poblar ElapsedMillis igual que el resto de los
        // endpoints. Sin este setup, GetAssociationsByRaceAsync sin mockear devuelve
        // null y GetStartUtcByCategoryIdAsync revienta con ArgumentNullException al
        // armar el diccionario — mismo problema ya documentado en ResultVoidTests.
        _raceCategories.Setup(rc => rc.GetAssociationsByRaceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RaceCategory>());
    }

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object, _notifier.Object, _raceCategories.Object, _adminNotifications.Object);

    private (Result a, Result b) DorsalDuplicateGroup()
    {
        // `a` retiene RunnerId=3 (así queda una fila que "ganó" el dorsal antes de que
        // la disputa se abriera — igual que el lado existing de TryResolveDorsalAsync).
        // Postgres real exige liberarlo al anular `a`, o el update de `b` a ese mismo
        // RunnerId choca contra IX_Results_RaceId_RunnerId (bug real, PR2a Task 8).
        var a = new Result { Id = 10, RaceId = 1, Dorsal = "105", RunnerId = 3, CategoryId = 5, Estado = ResultEstado.Controversia, DisputeGroupId = 10, DisputeMotivo = Domain.Entities.DisputeMotivo.DorsalDuplicado, TiempoLlegada = DateTime.UtcNow.AddMinutes(-5) };
        var b = new Result { Id = 11, RaceId = 1, Dorsal = null, DorsalPropuesto = "105", CategoryId = null, Estado = ResultEstado.Controversia, DisputeGroupId = 10, DisputeMotivo = Domain.Entities.DisputeMotivo.DorsalDuplicado, TiempoLlegada = DateTime.UtcNow.AddMinutes(-3) };
        _results.Setup(r => r.GetDisputedByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([a, b]);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "C", JoinCode = "X", Estado = RaceStatus.EnCurso });
        _results.Setup(r => r.GetAllByCategoryAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _runners.Setup(r => r.GetByDorsalAsync(1, "105", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Runner { Id = 3, RaceId = 1, Nombre = "Corredor", Dorsal = "105", CategoryId = 5 });
        return (a, b);
    }

    [Fact]
    public async Task ResolveAsync_AsignaAUnoYAnulaElOtro_LimpiaAmbosLados()
    {
        var (a, b) = DorsalDuplicateGroup();
        var request = new ResolveDisputeGroupRequest(
            Asignaciones: [new DisputeAssignment(11, "105")],
            Anular: [10],
            Razon: "El juez B confirmó por video");

        await BuildService().ResolveDisputeAsync(1, 10, request, AdminId);

        Assert.Equal(ResultEstado.Anulado, a.Estado);
        Assert.Equal(ResultEstado.Valido, b.Estado);
        Assert.Equal("105", b.Dorsal);
        Assert.Equal(3, b.RunnerId);
        Assert.Null(b.DorsalPropuesto);
        Assert.Null(b.DisputeGroupId);
        Assert.Null(b.DisputeMotivo);
        Assert.Null(a.DisputeGroupId);
        // El lado anulado debe soltar el RunnerId/Dorsal que retenía — si no, esta
        // misma reasignación (RunnerId=3 a `b`, arriba) choca contra
        // IX_Results_RaceId_RunnerId en Postgres real (bug real, PR2a Task 8).
        Assert.Null(a.RunnerId);
        Assert.Null(a.Dorsal);
    }

    [Fact]
    public async Task ResolveAsync_GrupoInexistente_LanzaNotFound()
    {
        _results.Setup(r => r.GetDisputedByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "C", JoinCode = "X" });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().ResolveDisputeAsync(1, 999, new ResolveDisputeGroupRequest([], [], "razon"), AdminId));
    }

    [Fact]
    public async Task ResolveAsync_MotivoCategoriaSinSalida_LanzaValidation()
    {
        var result = new Result { Id = 20, RaceId = 1, Estado = ResultEstado.Controversia, DisputeMotivo = Domain.Entities.DisputeMotivo.CategoriaSinSalida, DorsalPropuesto = "9", TiempoLlegada = DateTime.UtcNow };
        _results.Setup(r => r.GetDisputedByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([result]);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race { Id = 1, Nombre = "C", JoinCode = "X" });

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().ResolveDisputeAsync(1, 20, new ResolveDisputeGroupRequest([], [], "razon"), AdminId));

        Assert.Contains("StartUtc", ex.Message);
    }
}
