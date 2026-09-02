using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

/// <summary>
/// El lado destructivo del reinicio por salida en falso: qué llegadas se lleva puestas y,
/// sobre todo, cuáles NO.
/// </summary>
public class ResultVoidForResetTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _dashboardNotifier = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<IAdminNotificationService> _adminNotifications = new();

    private const int AdminId = 7;
    private const string Razon = "Salida en falso";

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object,
            _dashboardNotifier.Object, _raceCategories.Object, _adminNotifications.Object);

    private static Result Captura(int id, int? categoryId, ResultEstado estado = ResultEstado.Valido) => new()
    {
        Id = id,
        RaceId = 1,
        CategoryId = categoryId,
        RunnerId = id * 10,
        Dorsal = id.ToString(),
        Posicion = id,
        Estado = estado,
        TiempoLlegada = DateTime.UtcNow
    };

    private void Existen(params Result[] results) =>
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());

    [Fact]
    public async Task VoidForResetAsync_AnulaLasDeLaCategoriaYLiberaDorsalYCorredor()
    {
        var dentro = Captura(1, categoryId: 3);
        Existen(dentro);

        var anuladas = await BuildService().VoidForResetAsync(1, [3], false, AdminId, Razon);

        Assert.Equal(1, anuladas);
        Assert.Equal(ResultEstado.Anulado, dentro.Estado);
        Assert.Null(dentro.Dorsal);
        Assert.Null(dentro.RunnerId);
        Assert.Equal(0, dentro.Posicion);
    }

    [Fact]
    public async Task VoidForResetAsync_NoTocaLasDeOtraCategoria()
    {
        var otra = Captura(2, categoryId: 7);
        Existen(Captura(1, categoryId: 3), otra);

        await BuildService().VoidForResetAsync(1, [3], false, AdminId, Razon);

        Assert.Equal(ResultEstado.Valido, otra.Estado);
        Assert.Equal("2", otra.Dorsal);
    }

    // Sin dorsal no se sabe de qué categoría es. Reiniciar una sola categoría no puede
    // llevárselas: podrían pertenecer a cualquiera de las que siguen corriendo bien.
    [Fact]
    public async Task VoidForResetAsync_ReinicioParcial_NoTocaLasCapturasSinCategoria()
    {
        var sinDorsal = Captura(9, categoryId: null);
        Existen(Captura(1, categoryId: 3), sinDorsal);

        await BuildService().VoidForResetAsync(1, [3], incluirSinCategoria: false, AdminId, Razon);

        Assert.Equal(ResultEstado.Valido, sinDorsal.Estado);
    }

    [Fact]
    public async Task VoidForResetAsync_ReinicioTotal_TambienAnulaLasCapturasSinCategoria()
    {
        var sinDorsal = Captura(9, categoryId: null);
        Existen(Captura(1, categoryId: 3), sinDorsal);

        var anuladas = await BuildService().VoidForResetAsync(1, [3], incluirSinCategoria: true, AdminId, Razon);

        Assert.Equal(2, anuladas);
        Assert.Equal(ResultEstado.Anulado, sinDorsal.Estado);
    }

    // Una disputa viva describe la intención de asignar un dorsal en una carrera que se va a
    // correr de nuevo: dejarla abierta sería ofrecerle al Admin resolver un tiempo que ya no existe.
    [Fact]
    public async Task VoidForResetAsync_CierraLasDisputasAbiertasDeLasCategoriasReiniciadas()
    {
        var enDisputa = Captura(1, categoryId: 3, estado: ResultEstado.Controversia);
        enDisputa.DorsalPropuesto = "77";
        enDisputa.DisputeMotivo = DisputeMotivo.DorsalDuplicado;
        enDisputa.DisputeGroupId = 5;
        Existen(enDisputa);

        await BuildService().VoidForResetAsync(1, [3], false, AdminId, Razon);

        Assert.Equal(ResultEstado.Anulado, enDisputa.Estado);
        Assert.Null(enDisputa.DorsalPropuesto);
        Assert.Null(enDisputa.DisputeMotivo);
        Assert.Null(enDisputa.DisputeGroupId);
    }

    [Fact]
    public async Task VoidForResetAsync_YaAnulada_NoLaCuentaDeNuevo()
    {
        Existen(Captura(1, categoryId: 3, estado: ResultEstado.Anulado));

        var anuladas = await BuildService().VoidForResetAsync(1, [3], false, AdminId, Razon);

        Assert.Equal(0, anuladas);
        _results.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VoidForResetAsync_SinRazon_LanzaValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().VoidForResetAsync(1, [3], false, AdminId, "   "));
    }

    [Fact]
    public async Task VoidForResetAsync_DejaUnResultAuditPorCadaLlegadaAnulada()
    {
        Existen(Captura(1, categoryId: 3));

        await BuildService().VoidForResetAsync(1, [3], false, AdminId, Razon);

        // Estado y Dorsal: los dos campos que cambiaron.
        _audits.Verify(a => a.AddAsync(
            It.Is<ResultAudit>(e => e.ActorUserId == AdminId && e.Razon == Razon),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
