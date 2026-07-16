using Moq;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Races;
using NicaRunner.Application.Races.Dtos;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RaceServiceTests
{
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly FakeAuditLogRepository _auditRepo = new();

    private RaceService BuildService() =>
        new(_races.Object, _raceCategories.Object, _categories.Object, new AuditService(_auditRepo));

    [Fact]
    public async Task UpdateAsync_CarreraInexistente_LanzaNotFound()
    {
        _races.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Race?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => BuildService().UpdateAsync(
            99, new UpdateRaceRequest("X", null, DateTime.UtcNow, RaceStatus.Planeada), currentUserId: 1));
    }

    [Fact]
    public async Task UpdateAsync_CambiaNombreYEstado_RegistraDosEntradasDeAuditoria()
    {
        var race = new Race
        {
            Id = 1, Nombre = "5K Managua", Descripcion = "Original", FechaCarrera = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Estado = RaceStatus.Planeada, AdminId = 1
        };
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        var request = new UpdateRaceRequest("5K Managua Centro", "Original", race.FechaCarrera, RaceStatus.EnCurso);
        await BuildService().UpdateAsync(1, request, currentUserId: 9);

        Assert.Equal("5K Managua Centro", race.Nombre);
        Assert.Equal(RaceStatus.EnCurso, race.Estado);

        Assert.Equal(2, _auditRepo.Entries.Count);
        Assert.Contains(_auditRepo.Entries, e =>
            e.EntityType == AuditEntityTypes.Race && e.EntityId == 1 && e.AutorId == 9 &&
            e.Campo == "Nombre" && e.ValorAnterior == "5K Managua" && e.ValorNuevo == "5K Managua Centro");
        Assert.Contains(_auditRepo.Entries, e =>
            e.Campo == "Estado" && e.ValorAnterior == "Planeada" && e.ValorNuevo == "EnCurso");
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
