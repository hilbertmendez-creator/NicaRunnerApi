using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Application.Auditing;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Registrations;
using NicaRunner.Application.Runners;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

// tasks.md 3.6: row validation + in-file dedup para ConfirmBulkAsync, mock-based (sin DB
// real) — mismo split que RegistrationServiceTests/RegistrationServiceIntegrationTests.
// El agotamiento de cupo a mitad de lote contra datos persistidos (D13) vive en
// RegistrationBulkConfirmIntegrationTests, no acá.
public class RegistrationBulkConfirmTests
{
    private readonly Mock<IRegistrationRepository> _registrations = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRunnerService> _runnerService = new();
    private readonly Mock<IExcelRegistrationParser> _excelParser = new();
    private readonly FakeAuditLogRepository _auditRepo = new();

    private RegistrationService BuildService() => new(
        _registrations.Object,
        Mock.Of<IRegistrationLinkRepository>(),
        _races.Object,
        Mock.Of<IRaceCategoryRepository>(),
        _runnerService.Object,
        new AuditService(_auditRepo),
        Options.Create(new RegistrationOptions()),
        _excelParser.Object);

    private static Race ExistingRace(int id = 1) =>
        new() { Id = id, Nombre = "Carrera", JoinCode = "ABC123", FechaCarrera = DateTime.UtcNow.AddDays(30) };

    private static Registration ComprobanteSubido(int id, int raceCategoryId = 10) => new()
    {
        Id = id,
        RaceId = 1,
        RaceCategoryId = raceCategoryId,
        Nombre = $"Corredor {id}",
        FechaNacimiento = new DateTime(1990, 1, 1),
        Estado = RegistrationStatus.ComprobanteSubido
    };

    private void SetupPendientes(params Registration[] pendientes) =>
        _registrations.Setup(r => r.GetAllByRaceAsync(1, RegistrationStatus.ComprobanteSubido, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendientes.ToList());

    [Fact]
    public async Task ConfirmBulkAsync_ArchivoCorrupto_LanzaValidationException()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingRace());
        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>())).Throws(new InvalidOperationException("archivo corrupto"));

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().ConfirmBulkAsync(1, Stream.Null, adminId: 9, CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmBulkAsync_RegistrationIdNoResuelveEnEstaCarrera_ReportaErrorDeFila()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingRace());
        SetupPendientes();
        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns([new ParsedConfirmRow(2, 999, "101")]);

        var result = await BuildService().ConfirmBulkAsync(1, Stream.Null, adminId: 9, CancellationToken.None);

        Assert.Equal(1, result.TotalFilas);
        Assert.Equal(0, result.Confirmadas);
        var error = Assert.Single(result.Errores);
        Assert.Equal(2, error.Fila);
        Assert.Contains("no existe", error.Motivo);
    }

    [Fact]
    public async Task ConfirmBulkAsync_RegistrationIdVacioOInvalido_ReportaErrorDeFila()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingRace());
        SetupPendientes();
        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns([new ParsedConfirmRow(2, null, "101")]);

        var result = await BuildService().ConfirmBulkAsync(1, Stream.Null, adminId: 9, CancellationToken.None);

        var error = Assert.Single(result.Errores);
        Assert.Contains("RegistrationId vacío o inválido", error.Motivo);
    }

    [Fact]
    public async Task ConfirmBulkAsync_DorsalVacio_ReportaErrorDeFilaSinLlamarTryClaim()
    {
        var registration = ComprobanteSubido(5);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingRace());
        SetupPendientes(registration);
        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns([new ParsedConfirmRow(2, 5, "")]);

        var result = await BuildService().ConfirmBulkAsync(1, Stream.Null, adminId: 9, CancellationToken.None);

        Assert.Equal(0, result.Confirmadas);
        Assert.Contains("Dorsal vacío", Assert.Single(result.Errores).Motivo);
        _registrations.Verify(r => r.TryClaimAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // design.md D11: la equivalencia numérica ("0101" == "101") también aplica al
    // deduplicado dentro del mismo archivo, no solo a la unicidad contra la DB.
    [Fact]
    public async Task ConfirmBulkAsync_DorsalDuplicadoEnElArchivo_ConfirmaLaPrimeraYReportaLaSegunda()
    {
        var reg1 = ComprobanteSubido(5);
        var reg2 = ComprobanteSubido(6);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingRace());
        SetupPendientes(reg1, reg2);
        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns([new ParsedConfirmRow(2, 5, "0101"), new ParsedConfirmRow(3, 6, "101")]);

        _registrations.Setup(r => r.TryClaimAsync(5, 9, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(reg1);
        _registrations.Setup(r => r.TryReserveSlotAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _runnerService.Setup(s => s.CreateFromRegistrationAsync(reg1, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Runner { RaceId = 1, Nombre = "X", Dorsal = "0101", CategoryId = 1 });

        var result = await BuildService().ConfirmBulkAsync(1, Stream.Null, adminId: 9, CancellationToken.None);

        Assert.Equal(1, result.Confirmadas);
        var error = Assert.Single(result.Errores);
        Assert.Equal(3, error.Fila);
        Assert.Contains("duplicado", error.Motivo);
        _registrations.Verify(r => r.TryClaimAsync(6, It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // registration-review spec.md "Partial success on mixed valid and invalid rows".
    [Fact]
    public async Task ConfirmBulkAsync_FilaValidaYFilaInvalida_ConfirmaUnaYReportaLaOtra()
    {
        var reg1 = ComprobanteSubido(5);
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingRace());
        SetupPendientes(reg1);
        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns([new ParsedConfirmRow(2, 5, "301"), new ParsedConfirmRow(3, 42, "302")]);

        _registrations.Setup(r => r.TryClaimAsync(5, 9, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _registrations.Setup(r => r.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(reg1);
        _registrations.Setup(r => r.TryReserveSlotAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _runnerService.Setup(s => s.CreateFromRegistrationAsync(reg1, "301", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Runner { RaceId = 1, Nombre = "X", Dorsal = "301", CategoryId = 1 });

        var result = await BuildService().ConfirmBulkAsync(1, Stream.Null, adminId: 9, CancellationToken.None);

        Assert.Equal(2, result.TotalFilas);
        Assert.Equal(1, result.Confirmadas);
        var error = Assert.Single(result.Errores);
        Assert.Equal(3, error.Fila);
        Assert.Contains("no existe", error.Motivo);
    }
}
