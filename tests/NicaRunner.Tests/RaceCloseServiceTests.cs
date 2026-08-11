using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceCloseServiceTests
{
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultService> _resultService = new();
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<INotificationSender> _emailSender = new();
    private readonly Mock<IAdminNotificationService> _adminNotifications = new();

    private const int RaceId = 1;
    private const int CapturistaId = 10;
    private const int AdministradorId = 20;

    private (RaceService Service, RaceCategoryService CategoryService) BuildServiceWithCategoryService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);
        _emailSender
            .Setup(s => s.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        var categoryService = new RaceCategoryService(
            _raceCategories.Object, _categories.Object, _races.Object, _runners.Object, _resultService.Object);

        var service = new RaceService(
            _races.Object,
            _raceCategories.Object,
            _categories.Object,
            _audit.Object,
            categoryService,
            _results.Object,
            _users.Object,
            [_emailSender.Object],
            _adminNotifications.Object,
            NullLogger<RaceService>.Instance);

        return (service, categoryService);
    }

    private RaceService BuildService() => BuildServiceWithCategoryService().Service;

    private static Category MakeCategory(int id, string nombre) => new()
    {
        Id = id, Codigo = nombre, NombreCategoria = nombre, Distancia = 5, EdadMinima = 0, EdadMaxima = 99, Orden = 1
    };

    private static RaceCategory MakeAssociation(int categoryId, string nombre, RaceCategoryStatus estado) => new()
    {
        Id = categoryId,
        RaceId = RaceId,
        CategoryId = categoryId,
        Category = MakeCategory(categoryId, nombre),
        Estado = estado
    };

    private static User MakeUser(int id, UserRole role, string? email = null) => new()
    {
        Id = id, Email = email ?? $"user{id}@test.com", Nombre = $"Usuario {id}", Role = role
    };

    private void SetupRace(Race race, List<RaceCategory> associations)
    {
        _races.Setup(r => r.GetByIdAsync(race.Id, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(rc => rc.GetAssociationsByRaceAsync(race.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(associations);
        _raceCategories
            .Setup(rc => rc.GetAssociationsByIdsAsync(race.Id, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .Returns((int _, IReadOnlyCollection<int> ids, CancellationToken _) =>
                Task.FromResult(associations.Where(a => ids.Contains(a.CategoryId)).ToList()));
    }

    private void SetupBlockers(int disputed = 0, int missingRunner = 0) =>
        _results.Setup(r => r.GetCloseBlockerCountsAsync(RaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RaceCloseBlockerCounts(disputed, missingRunner));

    private void SetupUser(User user) =>
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private void SetupAdmins(params User[] admins) =>
        _users.Setup(u => u.GetByRoleAsync(UserRole.Administrador, It.IsAny<CancellationToken>())).ReturnsAsync(admins.ToList());

    [Fact]
    public async Task CloseAsync_Durabilidad_UnaTransicionPosteriorRederivaElEstadoCorrectamente()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));

        var (service, categoryService) = BuildServiceWithCategoryService();
        await service.CloseAsync(RaceId, CapturistaId);

        Assert.Equal(RaceStatus.Terminada, race.Estado);
        Assert.Equal(RaceCategoryStatus.Terminada, association.Estado);

        // Transición POSTERIOR e independiente sobre la MISMA categoría: si CloseAsync
        // hubiera escrito race.Estado directamente en vez de cascadear, esta transición
        // (real, no mockeada) seguiría re-derivando desde el estado real de la categoría —
        // la prueba de que Estado nunca quedó "pegado" a un valor puesto a mano.
        await categoryService.ReopenAsync(RaceId, new(CategoryIds: [5]), AdministradorId);

        Assert.Equal(RaceStatus.EnCurso, race.Estado);
    }

    [Fact]
    public async Task CloseAsync_ConDisputaAbierta_Lanza409NombrandoLaDisputa()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers(disputed: 1);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => BuildService().CloseAsync(RaceId, CapturistaId));

        Assert.Contains("controversia", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloseAsync_ConCapturaSinDorsal_Lanza409NombrandoLaCapturaFaltante()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers(missingRunner: 1);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => BuildService().CloseAsync(RaceId, CapturistaId));

        Assert.Contains("dorsal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloseAsync_ResultadoAnuladoSinDorsal_NoBloqueaElCierre()
    {
        // Corrección A: el repositorio (GetCloseBlockerCountsAsync, T3.1) excluye Anulado
        // de MissingRunner en la consulta SQL — acá se simula el resultado de esa consulta
        // ya filtrada (MissingRunner=0) para verificar que RaceService no bloquea el
        // cierre cuando el conteo del repositorio no reporta ese blocker.
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers(disputed: 0, missingRunner: 0);
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));

        var dto = await BuildService().CloseAsync(RaceId, CapturistaId);

        Assert.Equal(RaceStatus.Terminada, dto.Estado);
    }

    [Fact]
    public async Task CloseAsync_ConCategoriaPlaneada_Lanza409YNoLaTransicionaComoEfectoSecundario()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.Planeada, AdminId = 1 };
        var enCurso = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        var planeada = MakeAssociation(6, "10K", RaceCategoryStatus.Planeada);
        SetupRace(race, [enCurso, planeada]);
        SetupBlockers();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => BuildService().CloseAsync(RaceId, CapturistaId));

        Assert.Contains("10K", ex.Message);
        Assert.Equal(RaceCategoryStatus.Planeada, planeada.Estado);
        Assert.Equal(RaceCategoryStatus.EnCurso, enCurso.Estado);
        _raceCategories.Verify(rc => rc.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseAsync_ConLosTresBlockers_LosReportaTodosEnUn409()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.Planeada, AdminId = 1 };
        var planeada = MakeAssociation(6, "10K", RaceCategoryStatus.Planeada);
        SetupRace(race, [planeada]);
        SetupBlockers(disputed: 2, missingRunner: 3);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => BuildService().CloseAsync(RaceId, CapturistaId));

        Assert.Contains("10K", ex.Message);
        Assert.Contains("controversia", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dorsal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloseAsync_SinAusentesEnElRoster_NuncaBloquean()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));

        var dto = await BuildService().CloseAsync(RaceId, CapturistaId);

        Assert.Equal(RaceStatus.Terminada, dto.Estado);
        _runners.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CloseAsync_CarreraYaTerminada_EsIdempotente()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.Terminada, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.Terminada);
        SetupRace(race, [association]);
        SetupBlockers();

        var dto = await BuildService().CloseAsync(RaceId, CapturistaId);

        Assert.Equal(RaceStatus.Terminada, dto.Estado);
        Assert.Equal(RaceCategoryStatus.Terminada, association.Estado);
        _audit.Verify(
            a => a.TrackChanges(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<FieldChange>>()),
            Times.Never);
        _emailSender.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CloseAsync_RegistraElCampoEstadoYCerradaPorEnUnaSolaLlamada()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));

        List<FieldChange>? captured = null;
        _audit
            .Setup(a => a.TrackChanges(AuditEntityTypes.Race, RaceId, CapturistaId, It.IsAny<IEnumerable<FieldChange>>()))
            .Callback<string, int, int, IEnumerable<FieldChange>>((_, _, _, changes) => captured = changes.ToList());

        await BuildService().CloseAsync(RaceId, CapturistaId);

        _audit.Verify(
            a => a.TrackChanges(AuditEntityTypes.Race, RaceId, CapturistaId, It.IsAny<IEnumerable<FieldChange>>()),
            Times.Once);
        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Count);
        Assert.Contains(captured, c => c.Campo == "Estado" && c.ValorAnterior == "EnCurso" && c.ValorNuevo == "Terminada");
        Assert.Contains(captured, c => c.Campo == "CerradaPor" && c.ValorAnterior == null && c.ValorNuevo == "Capturista");
    }

    [Fact]
    public async Task CloseAsync_ActorCapturista_NotificaATodosLosAdministradores()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));
        var admin1 = MakeUser(1001, UserRole.Administrador, "admin1@test.com");
        var admin2 = MakeUser(1002, UserRole.Administrador, "admin2@test.com");
        SetupAdmins(admin1, admin2);

        await BuildService().CloseAsync(RaceId, CapturistaId);

        _emailSender.Verify(s => s.SendAsync(
            admin1.Email, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(s => s.SendAsync(
            admin2.Email, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CloseAsync_ActorAdministrador_NoEnviaEmail()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers();
        SetupUser(MakeUser(AdministradorId, UserRole.Administrador));

        await BuildService().CloseAsync(RaceId, AdministradorId);

        _emailSender.Verify(s => s.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseAsync_SenderDevuelveFallo_NoRompeElCierre()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));
        SetupAdmins(MakeUser(1001, UserRole.Administrador));
        _emailSender
            .Setup(s => s.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(false, "Proveedor caído"));

        var dto = await BuildService().CloseAsync(RaceId, CapturistaId);

        Assert.Equal(RaceStatus.Terminada, dto.Estado);
    }

    [Fact]
    public async Task CloseAsync_SenderLanzaExcepcion_NoRompeElCierre()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));
        SetupAdmins(MakeUser(1001, UserRole.Administrador));
        _emailSender
            .Setup(s => s.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Proveedor caído"));

        var dto = await BuildService().CloseAsync(RaceId, CapturistaId);

        Assert.Equal(RaceStatus.Terminada, dto.Estado);
    }

    [Fact]
    public async Task CloseAsync_CierraUnCapturista_RegistraLaNotificacionEnLaBandejaDelAdmin()
    {
        var race = new Race { Id = RaceId, Nombre = "5K Managua", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        SetupRace(race, [MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso)]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));
        SetupAdmins(MakeUser(1001, UserRole.Administrador));

        await BuildService().CloseAsync(RaceId, CapturistaId);

        _adminNotifications.Verify(
            n => n.NotifyAsync(
                AdminNotificationType.CarreraCerradaPorJuez,
                It.Is<string>(m => m.Contains("5K Managua") && m.Contains("Usuario 10")),
                RaceId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CloseAsync_CierraUnAdministrador_NoRegistraNotificacion()
    {
        // Un admin que cierra no necesita avisarse a sí mismo — mismo criterio que el email.
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        SetupRace(race, [MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso)]);
        SetupBlockers();
        SetupUser(MakeUser(AdministradorId, UserRole.Administrador));

        await BuildService().CloseAsync(RaceId, AdministradorId);

        _adminNotifications.Verify(
            n => n.NotifyAsync(It.IsAny<AdminNotificationType>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CloseAsync_CarreraYaTerminada_NoRegistraNotificacion()
    {
        // Cierre idempotente (design D4): no hay nada nuevo que cerrar, no hay nada que avisar.
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.Terminada, AdminId = 1 };
        SetupRace(race, [MakeAssociation(5, "5K", RaceCategoryStatus.Terminada)]);
        SetupBlockers();

        await BuildService().CloseAsync(RaceId, CapturistaId);

        _adminNotifications.Verify(
            n => n.NotifyAsync(It.IsAny<AdminNotificationType>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CloseAsync_SinSenderDeEmail_IgualRegistraLaNotificacionEnLaBandeja()
    {
        // La campana es el registro que el admin SIEMPRE ve. No puede quedar escondida
        // detrás de un proveedor de email ausente o caído: son dos canales independientes.
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        SetupRace(race, [MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso)]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));
        SetupAdmins(MakeUser(1001, UserRole.Administrador));

        var categoryService = new RaceCategoryService(
            _raceCategories.Object, _categories.Object, _races.Object, _runners.Object, _resultService.Object);
        var service = new RaceService(
            _races.Object, _raceCategories.Object, _categories.Object, _audit.Object, categoryService,
            _results.Object, _users.Object, [], _adminNotifications.Object, NullLogger<RaceService>.Instance);

        await service.CloseAsync(RaceId, CapturistaId);

        _adminNotifications.Verify(
            n => n.NotifyAsync(
                AdminNotificationType.CarreraCerradaPorJuez, It.IsAny<string>(), RaceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CloseAsync_LaBandejaLanzaExcepcion_NoRompeElCierre()
    {
        // Best-effort igual que el email: la fila de bitácora ya persistida es el registro
        // durable del cierre — un fallo escribiendo la campana no puede tumbarlo.
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        SetupRace(race, [MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso)]);
        SetupBlockers();
        SetupUser(MakeUser(CapturistaId, UserRole.Capturista));
        SetupAdmins(MakeUser(1001, UserRole.Administrador));
        _adminNotifications
            .Setup(n => n.NotifyAsync(
                It.IsAny<AdminNotificationType>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Base caída"));

        var dto = await BuildService().CloseAsync(RaceId, CapturistaId);

        Assert.Equal(RaceStatus.Terminada, dto.Estado);
    }
}
