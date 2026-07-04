using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Runners;
using NicaRunner.Application.Runners.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class RunnerServiceTests
{
    private readonly Mock<IRunnerRepository> _runners = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IRaceCategoryRepository> _raceCategories = new();
    private readonly Mock<IExcelRunnerParser> _excelParser = new();

    private RunnerService BuildService() =>
        new(_runners.Object, _races.Object, _raceCategories.Object, _excelParser.Object);

    private static Race RaceOn(DateTime fechaCarrera, int id = 1) =>
        new() { Id = id, Nombre = "Carrera", JoinCode = "ABC123", FechaCarrera = fechaCarrera };

    private static Category JuvenilCategory(int id = 5) =>
        new() { Id = id, Codigo = "JUV", NombreCategoria = "Juvenil", EdadMinima = 13, EdadMaxima = 17 };

    private static CreateRunnerRequest RequestWithBirthdate(DateTime fechaNacimiento, int categoryId = 5) =>
        new("Ana", "Pérez", "101", null, null, Sexo.F, "Club X", fechaNacimiento, null, categoryId);

    [Fact]
    public async Task CreateAsync_ConFechaNacimiento_CalculaEdadRespectoALaFechaDeLaCarrera()
    {
        var race = RaceOn(new DateTime(2026, 6, 1));
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(c => c.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(JuvenilCategory());
        _runners.Setup(r => r.DorsalExistsAsync(1, "101", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Runner? created = null;
        _runners.Setup(r => r.AddAsync(It.IsAny<Runner>(), It.IsAny<CancellationToken>()))
            .Callback<Runner, CancellationToken>((r, _) => created = r)
            .Returns(Task.CompletedTask);
        _runners.Setup(r => r.GetByIdAsync(1, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Runner?)null);

        // Cumple 15 años el 2026-06-01, nació 2011-05-15 (ya cumplió ese año)
        var request = RequestWithBirthdate(new DateTime(2011, 5, 15));

        var dto = await BuildService().CreateAsync(1, request);

        Assert.NotNull(created);
        Assert.Equal(15, created!.Edad);
        Assert.Equal(15, dto.Edad);
    }

    [Fact]
    public async Task CreateAsync_CumpleaniosPosteriorALaCarrera_NoSumaElAnioAun()
    {
        var race = RaceOn(new DateTime(2026, 6, 1));
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        // Nació 2011-12-20: para la fecha de carrera (2026-06-01) todavía no cumple 15, tiene 14
        _raceCategories.Setup(c => c.GetByIdAsync(1, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 6, Codigo = "INF", NombreCategoria = "Infantil", EdadMinima = 6, EdadMaxima = 14 });
        _runners.Setup(r => r.DorsalExistsAsync(1, "101", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Runner? created = null;
        _runners.Setup(r => r.AddAsync(It.IsAny<Runner>(), It.IsAny<CancellationToken>()))
            .Callback<Runner, CancellationToken>((r, _) => created = r)
            .Returns(Task.CompletedTask);
        _runners.Setup(r => r.GetByIdAsync(1, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Runner?)null);

        var request = RequestWithBirthdate(new DateTime(2011, 12, 20), categoryId: 6);

        var dto = await BuildService().CreateAsync(1, request);

        Assert.Equal(14, created!.Edad);
        Assert.Equal(14, dto.Edad);
    }

    [Fact]
    public async Task CreateAsync_SinFechaNacimientoNiEdad_LanzaValidation()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(RaceOn(new DateTime(2026, 6, 1)));
        _raceCategories.Setup(c => c.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(JuvenilCategory());
        _runners.Setup(r => r.DorsalExistsAsync(1, "101", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new CreateRunnerRequest("Ana", null, "101", null, null, null, null, null, null, 5);

        await Assert.ThrowsAsync<ValidationException>(() => BuildService().CreateAsync(1, request));
    }

    [Fact]
    public async Task CreateAsync_EdadFueraDeRangoDeLaCategoria_LanzaValidation()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(RaceOn(new DateTime(2026, 6, 1)));
        _raceCategories.Setup(c => c.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(JuvenilCategory());
        _runners.Setup(r => r.DorsalExistsAsync(1, "101", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Nació en 1990: tiene 36 años, fuera del rango 13-17 de Juvenil
        var request = RequestWithBirthdate(new DateTime(1990, 1, 1));

        await Assert.ThrowsAsync<ValidationException>(() => BuildService().CreateAsync(1, request));
    }

    [Fact]
    public async Task CreateAsync_DorsalDuplicadoEnLaCarrera_LanzaConflict()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(RaceOn(new DateTime(2026, 6, 1)));
        _raceCategories.Setup(c => c.GetByIdAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(JuvenilCategory());
        _runners.Setup(r => r.DorsalExistsAsync(1, "101", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = RequestWithBirthdate(new DateTime(2011, 5, 15));

        await Assert.ThrowsAsync<ConflictException>(() => BuildService().CreateAsync(1, request));
    }

    [Fact]
    public async Task CreateAsync_CategoriaNoAsignadaALaCarrera_LanzaNotFound()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(RaceOn(new DateTime(2026, 6, 1)));
        _raceCategories.Setup(c => c.GetByIdAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var request = RequestWithBirthdate(new DateTime(2011, 5, 15), categoryId: 99);

        await Assert.ThrowsAsync<NotFoundException>(() => BuildService().CreateAsync(1, request));
    }

    [Fact]
    public async Task ImportFromExcelAsync_FilaValidaYFilaConCategoriaInexistente_ImportaUnaYReportaLaOtra()
    {
        var race = RaceOn(new DateTime(2026, 6, 1));
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        var category = JuvenilCategory();
        _raceCategories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([category]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>())).Returns(
        [
            new ParsedRunnerRow(2, "Ana", "Pérez", "101", null, null, "F", null, new DateTime(2011, 5, 15), "Juvenil"),
            new ParsedRunnerRow(3, "Beto", null, "102", null, null, "M", null, new DateTime(2011, 5, 15), "Categoría Inexistente"),
        ]);

        List<Runner>? added = null;
        _runners.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Runner>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Runner>, CancellationToken>((rs, _) => added = rs.ToList())
            .Returns(Task.CompletedTask);

        var result = await BuildService().ImportFromExcelAsync(1, Stream.Null);

        Assert.Equal(2, result.TotalFilas);
        Assert.Equal(1, result.Importados);
        Assert.Single(result.Errores);
        Assert.Equal(3, result.Errores[0].Fila);
        Assert.Contains("no existe", result.Errores[0].Motivo);

        Assert.NotNull(added);
        Assert.Single(added!);
        Assert.Equal("101", added![0].Dorsal);
        _runners.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportFromExcelAsync_DorsalDuplicadoDentroDelArchivo_ReportaErrorEnLaSegundaFila()
    {
        var race = RaceOn(new DateTime(2026, 6, 1));
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);

        var category = JuvenilCategory();
        _raceCategories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([category]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>())).Returns(
        [
            new ParsedRunnerRow(2, "Ana", null, "101", null, null, "F", null, new DateTime(2011, 5, 15), "Juvenil"),
            new ParsedRunnerRow(3, "Beto", null, "101", null, null, "M", null, new DateTime(2011, 5, 15), "Juvenil"),
        ]);

        _runners.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Runner>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await BuildService().ImportFromExcelAsync(1, Stream.Null);

        Assert.Equal(1, result.Importados);
        Assert.Single(result.Errores);
        Assert.Equal(3, result.Errores[0].Fila);
        Assert.Contains("duplicado", result.Errores[0].Motivo);
    }

    [Fact]
    public async Task ImportFromExcelAsync_NingunaFilaValida_NoLlamaAddRange()
    {
        var race = RaceOn(new DateTime(2026, 6, 1));
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _raceCategories.Setup(c => c.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _runners.Setup(r => r.GetAllByRaceAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        _excelParser.Setup(p => p.Parse(It.IsAny<Stream>())).Returns(
        [
            new ParsedRunnerRow(2, "", null, "", null, null, null, null, null, ""),
        ]);

        var result = await BuildService().ImportFromExcelAsync(1, Stream.Null);

        Assert.Equal(0, result.Importados);
        Assert.Single(result.Errores);
        _runners.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Runner>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
