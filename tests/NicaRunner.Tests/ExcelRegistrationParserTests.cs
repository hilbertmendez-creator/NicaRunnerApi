using ClosedXML.Excel;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Excel;

namespace NicaRunner.Tests;

// tasks.md 3.6: unit tests para ExcelRegistrationParser, modelados directamente sobre
// ExcelRunnerParserTests — mismas convenciones de ClosedXML, pero para el parser de
// confirm-bulk (design.md D12).
public class ExcelRegistrationParserTests
{
    private readonly ExcelRegistrationParser _parser = new();

    private static Registration Pendiente(int id, string nombre, string categoria, string? referencia = "REF-1") => new()
    {
        Id = id,
        Nombre = nombre,
        Apellidos = "Apellido",
        FechaNacimiento = new DateTime(1990, 1, 1),
        ReferenciaTransferencia = referencia,
        RaceCategory = new RaceCategory { Category = new Category { NombreCategoria = categoria } }
    };

    [Fact]
    public void GenerateTemplate_IncluyeEncabezadosYFilasDePendientesConDorsalEnBlanco()
    {
        var pendientes = new List<Registration> { Pendiente(1, "Ana", "Juvenil") };

        var bytes = _parser.GenerateTemplate(pendientes, []);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheets.First();

        Assert.Equal("RegistrationId", sheet.Cell(1, 1).GetString());
        Assert.Equal("Dorsal", sheet.Cell(1, 7).GetString());

        Assert.Equal(1, sheet.Cell(2, 1).GetValue<int>());
        Assert.Equal("Ana", sheet.Cell(2, 2).GetString());
        Assert.Equal("Juvenil", sheet.Cell(2, 4).GetString());
        Assert.True(string.IsNullOrEmpty(sheet.Cell(2, 7).GetString()));
    }

    // design.md D12: a diferencia de ExcelRunnerParser.GenerateTemplate (hoja oculta usada
    // como fuente de validación de un dropdown), la hoja de referencia acá es VISIBLE y no
    // es fuente de ninguna validación — la categoría ya viene fija desde la sumisión pública.
    [Fact]
    public void GenerateTemplate_IncluyeHojaDeReferenciaVisibleSinDropdown()
    {
        var referencia = new List<Category>
        {
            new() { Codigo = "5K", NombreCategoria = "5 Kilómetros", Distancia = 5m, EdadMinima = 18, EdadMaxima = 99 }
        };

        var bytes = _parser.GenerateTemplate([], referencia);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var referenceSheet = workbook.Worksheet("Categorías (referencia)");

        Assert.Equal(XLWorksheetVisibility.Visible, referenceSheet.Visibility);
        Assert.Equal("5K", referenceSheet.Cell(2, 1).GetString());
        Assert.Equal("5 Kilómetros", referenceSheet.Cell(2, 2).GetString());
    }

    [Fact]
    public void GenerateTemplate_SinReferencia_NoCreaHojaDeReferencia()
    {
        var bytes = _parser.GenerateTemplate([], []);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));

        Assert.False(workbook.Worksheets.TryGetWorksheet("Categorías (referencia)", out _));
    }

    [Fact]
    public void Parse_ExtraeRegistrationIdYDorsalOmiteFilasVaciasYNoTocaColumnasDeReferencia()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inscripciones");
        sheet.Cell(1, 1).Value = "RegistrationId";
        sheet.Cell(2, 1).Value = 10;
        sheet.Cell(2, 2).Value = "Ana"; // referencia, no se lee
        sheet.Cell(2, 7).Value = "101";
        // fila 3 vacía, debe omitirse
        sheet.Cell(4, 1).Value = 12;
        sheet.Cell(4, 7).Value = "102";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var rows = _parser.Parse(stream);

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].Fila);
        Assert.Equal(10, rows[0].RegistrationId);
        Assert.Equal("101", rows[0].Dorsal);
        Assert.Equal(4, rows[1].Fila);
        Assert.Equal(12, rows[1].RegistrationId);
    }

    // design.md D12/spec.md: RegistrationId en blanco o no parseable es un error de fila,
    // NUNCA un salto silencioso — la fila sigue apareciendo en el resultado con
    // RegistrationId=null para que el caller la reporte como inválida.
    [Fact]
    public void Parse_RegistrationIdNoParseable_DevuelveFilaConRegistrationIdNulo()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inscripciones");
        sheet.Cell(2, 1).Value = "no-es-un-numero";
        sheet.Cell(2, 7).Value = "101";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var rows = _parser.Parse(stream);

        var row = Assert.Single(rows);
        Assert.Null(row.RegistrationId);
        Assert.Equal("101", row.Dorsal);
    }

    [Fact]
    public void Parse_DorsalVacio_DevuelveFilaConDorsalVacioNoOmitida()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inscripciones");
        sheet.Cell(2, 1).Value = 10;
        sheet.Cell(2, 7).Value = string.Empty;
        sheet.Cell(2, 2).Value = "Ana"; // fila no vacía en general (col 2), aunque Dorsal esté vacío

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var rows = _parser.Parse(stream);

        var row = Assert.Single(rows);
        Assert.Equal(10, row.RegistrationId);
        Assert.Equal(string.Empty, row.Dorsal);
    }
}
