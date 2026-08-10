using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class StaleRaceSweepServiceTests
{
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<INotificationSender> _emailSender = new();

    private static readonly DateTime Now = DateTime.UtcNow;

    private StaleRaceSweepService BuildService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        _emailSender
            .Setup(s => s.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        return new StaleRaceSweepService(
            _races.Object, _results.Object, _users.Object, [_emailSender.Object], NullLogger<StaleRaceSweepService>.Instance);
    }

    private static Race MakeRace(int id, string nombre, DateTime? raceStartUtc) => new()
    {
        Id = id, Nombre = nombre, JoinCode = $"X{id}", Estado = RaceStatus.EnCurso, AdminId = 1, RaceStartUtc = raceStartUtc
    };

    private void SetupEnCursoRaces(params Race[] races) =>
        _races.Setup(r => r.GetByStatusAsync(RaceStatus.EnCurso, It.IsAny<CancellationToken>())).ReturnsAsync(races.ToList());

    private void SetupLastCaptureAt(Dictionary<int, DateTime> lastCaptureAtByRaceId) =>
        _results
            .Setup(r => r.GetLastCaptureAtByRaceIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastCaptureAtByRaceId);

    private static User MakeAdmin(int id, string email) => new()
    {
        Id = id, Email = email, Nombre = $"Admin {id}", Role = UserRole.Administrador
    };

    private void SetupAdmins(params User[] admins) =>
        _users.Setup(u => u.GetByRoleAsync(UserRole.Administrador, It.IsAny<CancellationToken>())).ReturnsAsync(admins.ToList());

    [Fact]
    public async Task RunAsync_UltimaCapturaHace13Horas_SeReportaComoStale()
    {
        var race = MakeRace(1, "5K", raceStartUtc: Now.AddDays(-1));
        SetupEnCursoRaces(race);
        SetupLastCaptureAt(new Dictionary<int, DateTime> { [1] = Now.AddHours(-13) });
        SetupAdmins();

        var result = await BuildService().RunAsync();

        Assert.Single(result.StaleRaces);
        Assert.Equal(1, result.StaleRaces[0].RaceId);
    }

    [Fact]
    public async Task RunAsync_UltimaCapturaHace11Horas_NoSeReporta()
    {
        var race = MakeRace(1, "5K", raceStartUtc: Now.AddDays(-1));
        SetupEnCursoRaces(race);
        SetupLastCaptureAt(new Dictionary<int, DateTime> { [1] = Now.AddHours(-11) });
        SetupAdmins();

        var result = await BuildService().RunAsync();

        Assert.Empty(result.StaleRaces);
    }

    [Fact]
    public async Task RunAsync_SinCapturasIniciadaHace13Horas_UsaRaceStartUtcComoFallback()
    {
        var race = MakeRace(1, "5K", raceStartUtc: Now.AddHours(-13));
        SetupEnCursoRaces(race);
        SetupLastCaptureAt(new Dictionary<int, DateTime>()); // sin resultados capturados
        SetupAdmins();

        var result = await BuildService().RunAsync();

        Assert.Single(result.StaleRaces);
        Assert.Equal(race.RaceStartUtc, result.StaleRaces[0].LastActivityUtc);
    }

    [Fact]
    public async Task RunAsync_SinCapturasIniciadaHace2Horas_NoSeReporta()
    {
        var race = MakeRace(1, "5K", raceStartUtc: Now.AddHours(-2));
        SetupEnCursoRaces(race);
        SetupLastCaptureAt(new Dictionary<int, DateTime>());
        SetupAdmins();

        var result = await BuildService().RunAsync();

        Assert.Empty(result.StaleRaces);
    }

    [Fact]
    public async Task RunAsync_SoloConsultaCarrerasEnCurso_PlaneadaYTerminadaNuncaPuedenReportarse()
    {
        SetupEnCursoRaces(); // ninguna EnCurso: Planeada/Terminada quedan estructuralmente afuera
        SetupLastCaptureAt(new Dictionary<int, DateTime>());
        SetupAdmins();

        var result = await BuildService().RunAsync();

        Assert.Empty(result.StaleRaces);
        _races.Verify(r => r.GetByStatusAsync(RaceStatus.EnCurso, It.IsAny<CancellationToken>()), Times.Once);
        _races.Verify(
            r => r.GetByStatusAsync(It.Is<RaceStatus>(s => s != RaceStatus.EnCurso), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_NuncaMutaNiCierraNingunaCarrera()
    {
        var race = MakeRace(1, "5K", raceStartUtc: Now.AddDays(-1));
        SetupEnCursoRaces(race);
        SetupLastCaptureAt(new Dictionary<int, DateTime> { [1] = Now.AddHours(-13) });
        SetupAdmins();

        await BuildService().RunAsync();

        Assert.Equal(RaceStatus.EnCurso, race.Estado);
        _races.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _races.Verify(r => r.Remove(It.IsAny<Race>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ConCarrerasStale_NotificaATodosLosAdministradores()
    {
        var race = MakeRace(1, "5K", raceStartUtc: Now.AddDays(-1));
        SetupEnCursoRaces(race);
        SetupLastCaptureAt(new Dictionary<int, DateTime> { [1] = Now.AddHours(-13) });
        var admin1 = MakeAdmin(1001, "admin1@test.com");
        var admin2 = MakeAdmin(1002, "admin2@test.com");
        SetupAdmins(admin1, admin2);

        await BuildService().RunAsync();

        _emailSender.Verify(s => s.SendAsync(
            admin1.Email, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(s => s.SendAsync(
            admin2.Email, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_SenderLanzaExcepcion_NoRompeElBarrido_DevuelveLaListaIgual()
    {
        var race = MakeRace(1, "5K", raceStartUtc: Now.AddDays(-1));
        SetupEnCursoRaces(race);
        SetupLastCaptureAt(new Dictionary<int, DateTime> { [1] = Now.AddHours(-13) });
        SetupAdmins(MakeAdmin(1001, "admin1@test.com"));
        _emailSender
            .Setup(s => s.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Proveedor caído"));

        var result = await BuildService().RunAsync();

        Assert.Single(result.StaleRaces);
    }

    [Fact]
    public async Task RunAsync_SinCarrerasStale_NoNotificaANadie()
    {
        var race = MakeRace(1, "5K", raceStartUtc: Now.AddHours(-2));
        SetupEnCursoRaces(race);
        SetupLastCaptureAt(new Dictionary<int, DateTime> { [1] = Now.AddHours(-2) });
        SetupAdmins(MakeAdmin(1001, "admin1@test.com"));

        await BuildService().RunAsync();

        _emailSender.Verify(s => s.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
