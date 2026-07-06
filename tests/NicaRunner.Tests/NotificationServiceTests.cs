using Moq;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Notifications;

namespace NicaRunner.Tests;

public class NotificationServiceTests
{
    private readonly Mock<INotificationLogRepository> _logs = new();
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<INotificationSender> _emailSender = new();
    private readonly IEmailTemplateRenderer _emailRenderer = new EmailTemplateRenderer();

    private NotificationService BuildService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        return new(_logs.Object, _results.Object, _runners.Object, _races.Object, [_emailSender.Object], _emailRenderer);
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

    private void SetupResultContext(Result result, Runner runner)
    {
        _results.Setup(r => r.GetByIdAsync(result.Id, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _races.Setup(r => r.GetByIdAsync(result.RaceId, It.IsAny<CancellationToken>())).ReturnsAsync(MakeRace());
        _runners.Setup(r => r.GetByIdAsync(result.RaceId, runner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(runner);
    }

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
        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _logs.Verify(l => l.AddAsync(It.Is<NotificationLog>(n => n.Status == NotificationStatus.Pendiente && n.Mensaje.Contains("Juan")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyResultAsync_EnviaDeInmediatoConHtml()
    {
        var runner = MakeRunner();
        var result = MakeResult(runnerId: runner.Id);
        SetupResultContext(result, runner);
        _emailSender.Setup(s => s.SendAsync(runner.Email!, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        var dtos = await BuildService().NotifyResultAsync(100);

        Assert.Single(dtos);
        Assert.Equal(NotificationStatus.Enviada, dtos[0].Status);
        _emailSender.Verify(s => s.SendAsync(
            runner.Email!,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.Is<string?>(h => h != null && h.Contains("#0D47A1")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

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
        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingAsync_EnviaLosQueDevuelveElRepositorioYCuentaResultados()
    {
        var runnerOk = MakeRunner(id: 1, email: "ok@test.com");
        var runnerFalla = MakeRunner(id: 2, email: "falla@test.com");
        var resultOk = MakeResult(id: 100, runnerId: 1);
        var resultFalla = MakeResult(id: 101, runnerId: 2);

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
        SetupResultContext(resultOk, runnerOk);
        SetupResultContext(resultFalla, runnerFalla);
        _emailSender.Setup(s => s.SendAsync("ok@test.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));
        _emailSender.Setup(s => s.SendAsync("falla@test.com", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
