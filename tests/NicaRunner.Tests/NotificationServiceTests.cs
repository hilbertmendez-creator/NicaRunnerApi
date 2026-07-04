using Moq;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class NotificationServiceTests
{
    private readonly Mock<INotificationLogRepository> _logs = new();
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<INotificationSender> _emailSender = new();

    private NotificationService BuildService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        return new(_logs.Object, _results.Object, _runners.Object, _races.Object, [_emailSender.Object]);
    }

    private static Race MakeRace(int id = 1) => new() { Id = id, Nombre = "Carrera Test", AdminId = 1 };

    private static Runner MakeRunner(int id = 10, string? email = "runner@test.com", string? telefono = null) => new()
    {
        Id = id,
        RaceId = 1,
        Nombre = "Juan",
        Dorsal = "101",
        Email = email,
        Telefono = telefono,
        CategoryId = 1
    };

    private static Result MakeResult(int id = 100, int runnerId = 10) => new()
    {
        Id = id,
        RaceId = 1,
        RunnerId = runnerId,
        CategoryId = 1,
        Posicion = 1,
        TiempoLlegada = new DateTime(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc)
    };

    // NotifyAllAsync debe encolar (Pendiente) y NUNCA llamar al sender — el
    // envío real lo hace ProcessPendingAsync en el barrido, no el request.
    [Fact]
    public async Task NotifyAllAsync_SoloEncola_NuncaLlamaAlSender()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeRace());
        var runner = MakeRunner();
        _results.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeResult(runnerId: runner.Id)]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([runner]);

        var summary = await BuildService().NotifyAllAsync(1);

        Assert.Equal(1, summary.TotalResultados);
        Assert.Equal(1, summary.NotificacionesCreadas);
        Assert.Equal(0, summary.Enviadas);
        Assert.Equal(0, summary.Fallidas);
        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _logs.Verify(l => l.AddAsync(It.Is<NotificationLog>(n => n.Status == NotificationStatus.Pendiente), It.IsAny<CancellationToken>()), Times.Once);
    }

    // NotifyResultAsync (un solo resultado) sigue enviando de inmediato —
    // no tiene el riesgo de timeout del caso masivo.
    [Fact]
    public async Task NotifyResultAsync_EnviaDeInmediato()
    {
        var runner = MakeRunner();
        var result = MakeResult(runnerId: runner.Id);
        _results.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeRace());
        _runners.Setup(r => r.GetByIdAsync(1, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(runner);
        _emailSender.Setup(s => s.SendAsync(runner.Email!, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        var dtos = await BuildService().NotifyResultAsync(100);

        Assert.Single(dtos);
        Assert.Equal(NotificationStatus.Enviada, dtos[0].Status);
        _emailSender.Verify(s => s.SendAsync(runner.Email!, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Corredor sin email ni teléfono: se marca Fallida de inmediato, sin
    // intentar ningún envío, y con IntentosEnvio agotado para que el barrido
    // no lo siga reintentando en vano.
    [Fact]
    public async Task NotifyResultAsync_SinContacto_MarcaFallidaSinReintentos()
    {
        var runner = MakeRunner(email: null, telefono: null);
        var result = MakeResult(runnerId: runner.Id);
        _results.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeRace());
        _runners.Setup(r => r.GetByIdAsync(1, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(runner);

        var dtos = await BuildService().NotifyResultAsync(100);

        Assert.Single(dtos);
        Assert.Equal(NotificationStatus.Fallida, dtos[0].Status);
        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ProcessPendingAsync: envía lo que el repositorio le da (ya filtrado por
    // Pendiente/Fallida-reintentable), incrementa IntentosEnvio, y refleja
    // éxitos/fallos en el resumen.
    [Fact]
    public async Task ProcessPendingAsync_EnviaLosQueDevuelveElRepositorioYCuentaResultados()
    {
        var runnerOk = MakeRunner(id: 1, email: "ok@test.com");
        var runnerFalla = MakeRunner(id: 2, email: "falla@test.com");

        var logPendiente = new NotificationLog
        {
            Id = 1, RaceId = 1, RunnerId = 1, ResultId = 100,
            Channel = NotificationChannel.Email, Status = NotificationStatus.Pendiente,
            Mensaje = "hola", Runner = runnerOk
        };
        var logReintento = new NotificationLog
        {
            Id = 2, RaceId = 1, RunnerId = 2, ResultId = 101,
            Channel = NotificationChannel.Email, Status = NotificationStatus.Fallida, IntentosEnvio = 2,
            Mensaje = "hola", Runner = runnerFalla
        };

        _logs.Setup(l => l.GetPendingOrRetryableAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([logPendiente, logReintento]);
        _emailSender.Setup(s => s.SendAsync("ok@test.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));
        _emailSender.Setup(s => s.SendAsync("falla@test.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(false, "Resend caído"));

        var summary = await BuildService().ProcessPendingAsync();

        Assert.Equal(2, summary.Procesadas);
        Assert.Equal(1, summary.Enviadas);
        Assert.Equal(1, summary.Fallidas);
        Assert.Equal(NotificationStatus.Enviada, logPendiente.Status);
        Assert.Equal(1, logPendiente.IntentosEnvio);
        Assert.Equal(NotificationStatus.Fallida, logReintento.Status);
        Assert.Equal(3, logReintento.IntentosEnvio);
        Assert.Equal("Resend caído", logReintento.Error);
    }
}
