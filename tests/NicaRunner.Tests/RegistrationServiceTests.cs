using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Registrations;
using NicaRunner.Application.Registrations.Dtos;
using NicaRunner.Application.Runners;
using NicaRunner.Domain.Constants;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

// tasks.md 2.9: state machine, cierre/plazo vencido, confirm sin dorsal, reject sin
// Runner. Todo mock-based (sin DB real) — la integración contra Sqlite real de la
// concurrencia de capacidad vive en RegistrationServiceIntegrationTests (2.10).
public class RegistrationServiceTests
{
    private readonly Mock<IRegistrationRepository> _registrations = new();
    private readonly Mock<IRegistrationLinkRepository> _links = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<IRunnerService> _runnerService = new();
    private readonly FakeAuditLogRepository _auditRepo = new();

    private RegistrationService BuildService(RegistrationOptions? options = null) =>
        new(_registrations.Object, _links.Object, _races.Object, _raceCategories.Object,
            _runnerService.Object, new AuditService(_auditRepo), Options.Create(options ?? new RegistrationOptions()));

    private static Race OpenRace(int id = 1, DateTime? deadline = null) => new()
    {
        Id = id,
        Nombre = "Carrera",
        JoinCode = "ABC123",
        FechaCarrera = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        InscripcionesAbiertas = true,
        FechaLimiteInscripcion = deadline
    };

    private static RegistrationLink ActiveLink(int raceId = 1, string token = "tok") => new()
    {
        Id = 1,
        RaceId = raceId,
        Token = token,
        IsExpired = false,
        FechaExpiracion = DateTime.UtcNow.AddDays(30),
        CreatedBy = 1
    };

    private static Category JuvenilCategory(int id = 5) =>
        new() { Id = id, Codigo = "JUV", NombreCategoria = "Juvenil", EdadMinima = 13, EdadMaxima = 99, Distancia = 5m };

    private static RaceCategory RaceCategoryFor(Category category, int raceCategoryId = 10, int raceId = 1, decimal? precio = 100m) => new()
    {
        Id = raceCategoryId,
        RaceId = raceId,
        CategoryId = category.Id,
        Category = category,
        Precio = precio
    };

    private static SubmitRegistrationRequest ValidSubmit(DateTime fechaNacimiento, int raceCategoryId = 10, string? contactoNombre = null, string? contactoTelefono = null) =>
        new("Ana", "Pérez", "555-1234", "ana@x.com", Sexo.F, "Club X", fechaNacimiento, raceCategoryId, contactoNombre, contactoTelefono);

    // ---------- Public Link Access Validation ----------

    [Fact]
    public async Task SubmitAsync_LinkRevocado_LanzaNotFound()
    {
        var link = ActiveLink();
        link.IsExpired = true;
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(link);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().SubmitAsync("tok", ValidSubmit(new DateTime(2000, 1, 1)), CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_LinkExpiradoPorFecha_LanzaNotFound()
    {
        var link = ActiveLink();
        link.FechaExpiracion = DateTime.UtcNow.AddDays(-1);
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(link);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().SubmitAsync("tok", ValidSubmit(new DateTime(2000, 1, 1)), CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_CarreraConInscripcionesCerradas_LanzaConflict()
    {
        var race = OpenRace();
        race.InscripcionesAbiertas = false;
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().SubmitAsync("tok", ValidSubmit(new DateTime(2000, 1, 1)), CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_PlazoDeInscripcionVencido_LanzaConflict()
    {
        var race = OpenRace(deadline: DateTime.UtcNow.AddDays(-1));
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().SubmitAsync("tok", ValidSubmit(new DateTime(2000, 1, 1)), CancellationToken.None));
    }

    // ---------- Date of Birth Requirement ----------

    [Fact]
    public async Task SubmitAsync_FechaNacimientoFutura_LanzaValidation()
    {
        var race = OpenRace();
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().SubmitAsync("tok", ValidSubmit(DateTime.UtcNow.AddDays(1)), CancellationToken.None));
    }

    // ---------- Minor Emergency Contact Requirement ----------

    [Fact]
    public async Task SubmitAsync_MenorSinContactoEmergencia_LanzaValidation()
    {
        var race = OpenRace();
        var category = JuvenilCategory();
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(c => c.GetRaceCategoryByIdAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(RaceCategoryFor(category));

        // Nace 2013-06-01: cumple 12 años en la fecha de la carrera (2026-06-01) -> menor.
        var request = ValidSubmit(new DateTime(2013, 6, 1));

        await Assert.ThrowsAsync<ValidationException>(() => BuildService().SubmitAsync("tok", request, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_MenorConContactoEmergencia_CreaPendiente()
    {
        var race = OpenRace();
        var category = JuvenilCategory();
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(c => c.GetRaceCategoryByIdAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(RaceCategoryFor(category));

        Registration? added = null;
        _registrations.Setup(r => r.AddAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()))
            .Callback<Registration, CancellationToken>((r, _) => added = r)
            .Returns(Task.CompletedTask);

        var request = ValidSubmit(new DateTime(2013, 6, 1), contactoNombre: "Mamá", contactoTelefono: "555-0000");
        var dto = await BuildService().SubmitAsync("tok", request, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(RegistrationStatus.Pendiente, added!.Estado);
        Assert.Equal("Mamá", added.ContactoEmergenciaNombre);
        Assert.Equal(RegistrationStatus.Pendiente, dto.Estado);
        _registrations.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_Adulto_NoRequiereContactoEmergenciaYSeDescartaSiSeEnvia()
    {
        var race = OpenRace();
        var category = JuvenilCategory();
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(c => c.GetRaceCategoryByIdAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(RaceCategoryFor(category));

        Registration? added = null;
        _registrations.Setup(r => r.AddAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()))
            .Callback<Registration, CancellationToken>((r, _) => added = r)
            .Returns(Task.CompletedTask);

        // Nace 1990: adulto en 2026 -> no exige contacto de emergencia, aunque se envíe.
        var request = ValidSubmit(new DateTime(1990, 1, 1), contactoNombre: "No debería guardarse");

        await BuildService().SubmitAsync("tok", request, CancellationToken.None);

        Assert.Null(added!.ContactoEmergenciaNombre);
    }

    // ---------- Submission not capacity-gated ----------

    [Fact]
    public async Task SubmitAsync_NuncaConsultaCapacidad()
    {
        var race = OpenRace();
        var category = JuvenilCategory();
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(c => c.GetRaceCategoryByIdAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(RaceCategoryFor(category));

        await BuildService().SubmitAsync("tok", ValidSubmit(new DateTime(1990, 1, 1)), CancellationToken.None);

        _registrations.Verify(r => r.TryReserveSlotAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Receipt Upload and State Transition ----------

    [Fact]
    public async Task UploadReceiptAsync_DesdePendiente_TransicionaAComprobanteSubido()
    {
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(OpenRace());

        var registration = new Registration { Id = 5, RaceId = 1, RaceCategoryId = 10, Estado = RegistrationStatus.Pendiente, FechaNacimiento = new DateTime(1990, 1, 1) };
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        var dto = await BuildService().UploadReceiptAsync("tok", 5, new UploadReceiptRequest("REF-001"), CancellationToken.None);

        Assert.Equal(RegistrationStatus.ComprobanteSubido, dto.Estado);
        Assert.Equal("REF-001", dto.ReferenciaTransferencia);
    }

    [Theory]
    [InlineData(RegistrationStatus.ComprobanteSubido)]
    [InlineData(RegistrationStatus.Confirmada)]
    [InlineData(RegistrationStatus.Rechazada)]
    public async Task UploadReceiptAsync_FueraDePendiente_LanzaConflict(RegistrationStatus estado)
    {
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveLink());
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(OpenRace());

        var registration = new Registration { Id = 5, RaceId = 1, RaceCategoryId = 10, Estado = estado, FechaNacimiento = new DateTime(1990, 1, 1) };
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().UploadReceiptAsync("tok", 5, new UploadReceiptRequest("REF-001"), CancellationToken.None));
    }

    // ---------- Confirm ----------

    [Fact]
    public async Task ConfirmAsync_SinDorsal_LanzaValidationYNuncaLlamaTryClaim()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().ConfirmAsync(1, 5, new ConfirmRegistrationRequest(""), adminId: 9, CancellationToken.None));

        _registrations.Verify(r => r.TryClaimAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_FueraDeEstadoComprobanteSubido_LanzaConflict()
    {
        _registrations.Setup(r => r.TryClaimAsync(5, 9, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var registration = new Registration { Id = 5, RaceId = 1, RaceCategoryId = 10, Estado = RegistrationStatus.Pendiente, FechaNacimiento = new DateTime(1990, 1, 1) };
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().ConfirmAsync(1, 5, new ConfirmRegistrationRequest("101"), adminId: 9, CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmAsync_RegistrationInexistente_LanzaNotFound()
    {
        _registrations.Setup(r => r.TryClaimAsync(5, 9, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync((Registration?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().ConfirmAsync(1, 5, new ConfirmRegistrationRequest("101"), adminId: 9, CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmAsync_CupoLleno_RevierteClaimYLanzaConflict()
    {
        var category = JuvenilCategory();
        var raceCategory = RaceCategoryFor(category);
        var registration = new Registration { Id = 5, RaceId = 1, RaceCategoryId = raceCategory.Id, RaceCategory = raceCategory, Estado = RegistrationStatus.ComprobanteSubido, FechaNacimiento = new DateTime(1990, 1, 1) };

        _registrations.Setup(r => r.TryClaimAsync(5, 9, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _registrations.Setup(r => r.TryReserveSlotAsync(raceCategory.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().ConfirmAsync(1, 5, new ConfirmRegistrationRequest("101"), adminId: 9, CancellationToken.None));

        _registrations.Verify(r => r.ReleaseClaimAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        _runnerService.Verify(s => s.CreateFromRegistrationAsync(It.IsAny<Registration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_Exitoso_CreaRunnerYRegistraAuditLog()
    {
        var category = JuvenilCategory();
        var raceCategory = RaceCategoryFor(category);
        var registration = new Registration { Id = 5, RaceId = 1, RaceCategoryId = raceCategory.Id, RaceCategory = raceCategory, Estado = RegistrationStatus.ComprobanteSubido, FechaNacimiento = new DateTime(1990, 1, 1) };
        var runner = new Runner { RaceId = 1, Nombre = "Ana", Dorsal = "101", CategoryId = category.Id };

        _registrations.Setup(r => r.TryClaimAsync(5, 9, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _registrations.Setup(r => r.TryReserveSlotAsync(raceCategory.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _runnerService.Setup(s => s.CreateFromRegistrationAsync(registration, "101", It.IsAny<CancellationToken>())).ReturnsAsync(runner);

        var dto = await BuildService().ConfirmAsync(1, 5, new ConfirmRegistrationRequest("101"), adminId: 9, CancellationToken.None);

        Assert.Same(runner, registration.Runner);
        Assert.Equal(2, _auditRepo.Entries.Count);
        Assert.Contains(_auditRepo.Entries, e =>
            e.EntityType == AuditEntityTypes.Registration && e.EntityId == 5 && e.AutorId == 9 &&
            e.Campo == "Estado" && e.ValorNuevo == nameof(RegistrationStatus.Confirmada));
        _registrations.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _registrations.Verify(r => r.ReleaseClaimAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _registrations.Verify(r => r.ReleaseSlotAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_PromoteFalla_RevierteReservaYClaim()
    {
        var category = JuvenilCategory();
        var raceCategory = RaceCategoryFor(category);
        var registration = new Registration { Id = 5, RaceId = 1, RaceCategoryId = raceCategory.Id, RaceCategory = raceCategory, Estado = RegistrationStatus.ComprobanteSubido, FechaNacimiento = new DateTime(1990, 1, 1) };

        _registrations.Setup(r => r.TryClaimAsync(5, 9, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _registrations.Setup(r => r.TryReserveSlotAsync(raceCategory.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _runnerService.Setup(s => s.CreateFromRegistrationAsync(registration, "101", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("dorsal ya tomado"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().ConfirmAsync(1, 5, new ConfirmRegistrationRequest("101"), adminId: 9, CancellationToken.None));

        _registrations.Verify(r => r.ReleaseSlotAsync(raceCategory.Id, It.IsAny<CancellationToken>()), Times.Once);
        _registrations.Verify(r => r.ReleaseClaimAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        _registrations.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Reject ----------

    [Theory]
    [InlineData(RegistrationStatus.Pendiente)]
    [InlineData(RegistrationStatus.ComprobanteSubido)]
    public async Task RejectAsync_DesdeEstadoElegible_TransicionaARechazadaSinCrearRunner(RegistrationStatus estado)
    {
        var registration = new Registration { Id = 5, RaceId = 1, RaceCategoryId = 10, Estado = estado, FechaNacimiento = new DateTime(1990, 1, 1) };
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        var dto = await BuildService().RejectAsync(1, 5, new RejectRegistrationRequest("Comprobante inválido"), adminId: 9, CancellationToken.None);

        Assert.Equal(RegistrationStatus.Rechazada, dto.Estado);
        Assert.Null(dto.RunnerId);
        Assert.Equal("Comprobante inválido", dto.MotivoRechazo);
        _runnerService.Verify(s => s.CreateFromRegistrationAsync(It.IsAny<Registration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(RegistrationStatus.Confirmada)]
    [InlineData(RegistrationStatus.Rechazada)]
    public async Task RejectAsync_DesdeEstadoNoElegible_LanzaConflict(RegistrationStatus estado)
    {
        var registration = new Registration { Id = 5, RaceId = 1, RaceCategoryId = 10, Estado = estado, FechaNacimiento = new DateTime(1990, 1, 1) };
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().RejectAsync(1, 5, new RejectRegistrationRequest("motivo"), adminId: 9, CancellationToken.None));
    }

    // ---------- Registration Link Administration (tasks.md 2.12 / 2.13) ----------

    [Fact]
    public async Task CreateLinkAsync_CarreraInexistente_LanzaNotFound()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Race?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().CreateLinkAsync(1, new CreateRegistrationLinkRequest(30), creatorId: 9, CancellationToken.None));
    }

    [Fact]
    public async Task CreateLinkAsync_SinCategoriaConPrecioConfigurado_LanzaConflictYNoCreaEnlace()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(OpenRace());
        _raceCategories.Setup(c => c.GetAllRaceCategoriesByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([RaceCategoryFor(JuvenilCategory(), precio: null)]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            BuildService().CreateLinkAsync(1, new CreateRegistrationLinkRequest(30), creatorId: 9, CancellationToken.None));

        _links.Verify(l => l.AddAsync(It.IsAny<RegistrationLink>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLinkAsync_ConAlMenosUnaCategoriaConPrecio_CreaEnlaceActivo()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(OpenRace());
        _raceCategories.Setup(c => c.GetAllRaceCategoriesByRaceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([RaceCategoryFor(JuvenilCategory())]);

        RegistrationLink? added = null;
        _links.Setup(l => l.AddAsync(It.IsAny<RegistrationLink>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrationLink, CancellationToken>((l, _) => added = l)
            .Returns(Task.CompletedTask);

        var dto = await BuildService().CreateLinkAsync(1, new CreateRegistrationLinkRequest(30), creatorId: 9, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(1, added!.RaceId);
        Assert.Equal(9, added.CreatedBy);
        Assert.False(dto.IsExpired);
        Assert.False(string.IsNullOrWhiteSpace(dto.Token));
        _links.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeLinkAsync_EnlaceActivo_LoInvalidaInmediatamenteParaAccesoAnonimo()
    {
        var link = ActiveLink();
        _links.Setup(l => l.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(link);
        _links.Setup(l => l.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(link);

        await BuildService().RevokeLinkAsync(1, 1, CancellationToken.None);

        Assert.True(link.IsExpired);
        _links.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().GetLinkInfoAsync("tok", CancellationToken.None));
    }

    [Fact]
    public async Task RevokeLinkAsync_EnlaceInexistente_LanzaNotFound()
    {
        _links.Setup(l => l.GetByIdAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync((RegistrationLink?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            BuildService().RevokeLinkAsync(1, 99, CancellationToken.None));
    }
}
