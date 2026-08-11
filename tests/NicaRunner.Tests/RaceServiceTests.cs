using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NicaRunner.Application.AdminNotifications;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Application.Results;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceServiceTests
{
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IResultService> _resultService = new();
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly FakeAuditLogRepository _auditRepo = new();

    private RaceService BuildService()
    {
        var categoryService = new RaceCategoryService(
            _raceCategories.Object, _categories.Object, _races.Object, _runners.Object, _resultService.Object);

        return new RaceService(
            _races.Object,
            _raceCategories.Object,
            _categories.Object,
            new AuditService(_auditRepo),
            categoryService,
            _results.Object,
            _users.Object,
            [],
            Mock.Of<IAdminNotificationService>(),
            NullLogger<RaceService>.Instance);
    }

    [Fact]
    public async Task UpdateAsync_CarreraInexistente_LanzaNotFound()
    {
        _races.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Race?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => BuildService().UpdateAsync(
            99, new UpdateRaceRequest("X", null, DateTime.UtcNow, RaceStatus.Planeada), currentUserId: 1));
    }

    [Fact]
    public async Task UpdateAsync_CambiaNombre_RegistraUnaEntradaDeAuditoria()
    {
        var race = new Race
        {
            Id = 1, Nombre = "5K Managua", Descripcion = "Original", FechaCarrera = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Estado = RaceStatus.Planeada, AdminId = 1
        };
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        var request = new UpdateRaceRequest("5K Managua Centro", "Original", race.FechaCarrera, RaceStatus.Planeada);
        await BuildService().UpdateAsync(1, request, currentUserId: 9);

        Assert.Equal("5K Managua Centro", race.Nombre);
        Assert.Equal(RaceStatus.Planeada, race.Estado);

        var entry = Assert.Single(_auditRepo.Entries);
        Assert.Equal(AuditEntityTypes.Race, entry.EntityType);
        Assert.Equal(1, entry.EntityId);
        Assert.Equal(9, entry.AutorId);
        Assert.Equal("Nombre", entry.Campo);
        Assert.Equal("5K Managua", entry.ValorAnterior);
        Assert.Equal("5K Managua Centro", entry.ValorNuevo);
    }

    // Correction B: Race.Estado es derivado (RaceStatusDeriver) — UpdateAsync ya no
    // puede escribirlo. Un valor CAMBIADO en el body ahora es un 400 que redirige al
    // endpoint correcto, en vez de un write silencioso que la próxima transición de
    // categoría revierte.
    [Fact]
    public async Task UpdateAsync_ConEstadoCambiado_LanzaValidationExceptionNombrandoCloseOReopen()
    {
        var race = new Race
        {
            Id = 1, Nombre = "5K", Descripcion = null, FechaCarrera = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Estado = RaceStatus.Planeada, AdminId = 1
        };
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        var request = new UpdateRaceRequest("5K", null, race.FechaCarrera, RaceStatus.EnCurso);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => BuildService().UpdateAsync(1, request, currentUserId: 9));

        Assert.Contains("close", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reopen", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RaceStatus.Planeada, race.Estado);
        Assert.Empty(_auditRepo.Entries);
    }

    [Fact]
    public async Task UpdateAsync_SinCambios_NoRegistraAuditoria()
    {
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var race = new Race { Id = 1, Nombre = "5K", Descripcion = null, FechaCarrera = fecha, Estado = RaceStatus.Planeada, AdminId = 1 };
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        var request = new UpdateRaceRequest("5K", null, fecha, RaceStatus.Planeada);
        await BuildService().UpdateAsync(1, request, currentUserId: 9);

        Assert.Empty(_auditRepo.Entries);
    }

    [Fact]
    public async Task UpdateAsync_DescripcionDeNullAValor_RegistraValorAnteriorNulo()
    {
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var race = new Race { Id = 1, Nombre = "5K", Descripcion = null, FechaCarrera = fecha, Estado = RaceStatus.Planeada, AdminId = 1 };
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        var request = new UpdateRaceRequest("5K", "Nueva descripción", fecha, RaceStatus.Planeada);
        await BuildService().UpdateAsync(1, request, currentUserId: 9);

        var entry = Assert.Single(_auditRepo.Entries);
        Assert.Equal("Descripcion", entry.Campo);
        Assert.Null(entry.ValorAnterior);
        Assert.Equal("Nueva descripción", entry.ValorNuevo);
    }
}
