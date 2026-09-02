using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Results;
using NicaRunner.Application.Results.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

/// <summary>
/// Captura offline: el instante de llegada puede venir del dispositivo cuando no hubo señal
/// para preguntarle al servidor. Espejo de RaceCategoryClientClockTests para el cero — si
/// una punta del tiempo oficial acepta el reloj del cliente con estas reglas, la otra no
/// puede aceptarlo con otras.
/// </summary>
public class ResultArrivalClockTests
{
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceDashboardNotifier> _dashboardNotifier = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<IAdminNotificationService> _adminNotifications = new();

    private static readonly DateTime RaceStart = DateTime.UtcNow.AddHours(-1);

    public ResultArrivalClockTests()
    {
        _raceCategories.Setup(rc => rc.GetAssociationsByRaceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RaceCategory>());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Race
            {
                Id = 1, Nombre = "Test", AdminId = 1,
                Estado = RaceStatus.EnCurso, RaceStartUtc = RaceStart
            });
    }

    private ResultService BuildService() =>
        new(_results.Object, _audits.Object, _races.Object, _runners.Object,
            _dashboardNotifier.Object, _raceCategories.Object, _adminNotifications.Object);

    /// <summary>El repo devuelve al releer la MISMA instancia que se le agregó, como EF.</summary>
    private void CapturaSePersiste()
    {
        Result? added = null;
        _results.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()))
            .Callback<Result, CancellationToken>((r, _) => { added = r; r.Id = 42; })
            .Returns(Task.CompletedTask);
        _results.Setup(r => r.GetByIdAsync(1, 42, It.IsAny<CancellationToken>())).ReturnsAsync(() => added);
    }

    [Fact]
    public async Task Create_SinTiempoDeCliente_UsaElRelojDelServidorYLoMarcaComoTal()
    {
        CapturaSePersiste();

        var antes = DateTime.UtcNow;
        var dto = await BuildService().CreateAsync(1, new CreateResultRequest(null), capturistaId: 5);

        Assert.Equal(TiempoLlegadaOrigen.Servidor, dto.TiempoOrigen);
        Assert.Null(dto.TiempoOffsetConfianzaMs);
        Assert.InRange(dto.TiempoLlegada, antes.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Create_ConTiempoDeClienteCalibrado_LoRespetaYMarcaCliente()
    {
        CapturaSePersiste();

        // Lo que importa: el tiempo que queda es el del cruce de meta, no el de ahora —
        // que es cuando volvió la señal y salió el POST.
        var cruce = DateTime.UtcNow.AddMinutes(-10);

        var dto = await BuildService().CreateAsync(
            1, new CreateResultRequest(null, cruce, OffsetConfianzaMs: 250), capturistaId: 5);

        Assert.Equal(cruce, dto.TiempoLlegada);
        Assert.Equal(TiempoLlegadaOrigen.Cliente, dto.TiempoOrigen);
        Assert.Equal(250, dto.TiempoOffsetConfianzaMs);
    }

    // Sin calibración se acepta igual, marcado fuerte: un tiempo dudoso y señalado es mejor
    // que perder la llegada. Mismo criterio que ClienteSinCalibrar en el cero.
    [Fact]
    public async Task Create_ConTiempoDeClienteSinCalibrar_LoAceptaMarcado()
    {
        CapturaSePersiste();

        var dto = await BuildService().CreateAsync(
            1, new CreateResultRequest(null, DateTime.UtcNow.AddMinutes(-3)), capturistaId: 5);

        Assert.Equal(TiempoLlegadaOrigen.ClienteSinCalibrar, dto.TiempoOrigen);
        Assert.Null(dto.TiempoOffsetConfianzaMs);
    }

    [Fact]
    public async Task Create_TiempoDeClienteEnElFuturo_LanzaValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => BuildService().CreateAsync(
            1, new CreateResultRequest(null, DateTime.UtcNow.AddMinutes(30)), capturistaId: 5));
    }

    [Fact]
    public async Task Create_TiempoDeClienteDemasiadoViejo_LanzaValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => BuildService().CreateAsync(
            1, new CreateResultRequest(null, DateTime.UtcNow.AddHours(-13)), capturistaId: 5));
    }

    // Nadie cruza la meta antes de largar. Es la misma regla que ya protege la edición
    // manual de TiempoLlegada, aplicada al camino offline.
    [Fact]
    public async Task Create_TiempoDeClienteAnteriorAlInicioDeLaCarrera_LanzaValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => BuildService().CreateAsync(
            1, new CreateResultRequest(null, RaceStart.AddMinutes(-1)), capturistaId: 5));
    }
}
