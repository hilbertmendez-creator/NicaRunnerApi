using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceReopenServiceTests
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

    private const int RaceId = 1;
    private const int AdministradorId = 20;

    private (RaceService Service, RaceCategoryService CategoryService) BuildServiceWithCategoryService()
    {
        _emailSender.Setup(s => s.Channel).Returns(NotificationChannel.Email);

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
            Mock.Of<IAdminNotificationService>(),
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

    [Fact]
    public async Task ReopenAsync_CascadeaSobreCategoriasTerminadaYRegistraElCambioDeEstado()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.Terminada, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.Terminada);
        SetupRace(race, [association]);

        List<FieldChange>? captured = null;
        _audit
            .Setup(a => a.TrackChanges(AuditEntityTypes.Race, RaceId, AdministradorId, It.IsAny<IEnumerable<FieldChange>>()))
            .Callback<string, int, int, IEnumerable<FieldChange>>((_, _, _, changes) => captured = changes.ToList());

        var dto = await BuildService().ReopenAsync(RaceId, AdministradorId);

        Assert.Equal(RaceStatus.EnCurso, dto.Estado);
        Assert.Equal(RaceCategoryStatus.EnCurso, association.Estado);
        _audit.Verify(
            a => a.TrackChanges(AuditEntityTypes.Race, RaceId, AdministradorId, It.IsAny<IEnumerable<FieldChange>>()),
            Times.Once);
        Assert.NotNull(captured);
        var change = Assert.Single(captured!);
        Assert.Equal("Estado", change.Campo);
        Assert.Equal("Terminada", change.ValorAnterior);
        Assert.Equal("EnCurso", change.ValorNuevo);
    }

    [Fact]
    public async Task ReopenAsync_Durabilidad_UnaTransicionPosteriorSigueRederivandoElEstado()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.Terminada, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.Terminada);
        SetupRace(race, [association]);

        var (service, categoryService) = BuildServiceWithCategoryService();
        await service.ReopenAsync(RaceId, AdministradorId);

        Assert.Equal(RaceStatus.EnCurso, race.Estado);

        // Transición POSTERIOR real (no mockeada) sobre la misma categoría: si reopen
        // hubiera escrito race.Estado directamente, esto seguiría derivando bien igual —
        // la prueba es que el vínculo con el estado real de la categoría nunca se rompió.
        await categoryService.CloseAsync(RaceId, new(CategoryIds: [5]), AdministradorId);

        Assert.Equal(RaceStatus.Terminada, race.Estado);
    }

    [Fact]
    public async Task ReopenAsync_CarreraYaEnCurso_EsIdempotenteYNoAuditaNada()
    {
        var race = new Race { Id = RaceId, Nombre = "5K", JoinCode = "X", Estado = RaceStatus.EnCurso, AdminId = 1 };
        var association = MakeAssociation(5, "5K", RaceCategoryStatus.EnCurso);
        SetupRace(race, [association]);

        var dto = await BuildService().ReopenAsync(RaceId, AdministradorId);

        Assert.Equal(RaceStatus.EnCurso, dto.Estado);
        Assert.Equal(RaceCategoryStatus.EnCurso, association.Estado);
        _audit.Verify(
            a => a.TrackChanges(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<FieldChange>>()),
            Times.Never);
    }
}
