using ClosedXML.Excel;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Excel;

namespace NicaRunner.Tests;

public class ExcelRunnerParserTests
{
    private readonly ExcelRunnerParser _parser = new();

    [Fact]
    public void GenerateTemplate_IncluyeEncabezadosYListaDeCategoriasComoValidacion()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, NombreCategoria = "Infantil" },
            new() { Id = 2, NombreCategoria = "Juvenil" },
        };

        var bytes = _parser.GenerateTemplate(categories);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheets.First();

        Assert.Equal("Nombre", sheet.Cell(1, 1).GetString());
        Assert.Equal("Categoría", sheet.Cell(1, 9).GetString());

        var lookupSheet = workbook.Worksheet("Categorías (no editar)");
        Assert.Equal("Infantil", lookupSheet.Cell(1, 1).GetString());
        Assert.Equal("Juvenil", lookupSheet.Cell(2, 1).GetString());
        Assert.True(lookupSheet.Visibility == XLWorksheetVisibility.Hidden || lookupSheet.Visibility == XLWorksheetVisibility.VeryHidden);

        // Regresión: una referencia directa a un rango de otra hoja como origen de una lista
        // de validación de datos hace que Excel pida reparar el archivo al abrirlo. El fix usa
        // un rango con nombre (defined name) como origen en su lugar.
        var definedName = Assert.Single(workbook.DefinedNames);
        Assert.True(definedName.IsValid);

        var categoriaValidation = Assert.Single(
            sheet.DataValidations,
            dv => dv.AllowedValues == XLAllowedValues.List && dv.MinValue.StartsWith('='));
        Assert.Equal($"={definedName.Name}", categoriaValidation.MinValue);
    }

    [Fact]
    public void GenerateTemplate_SinCategorias_NoCreaHojaDeCategorias()
    {
        var bytes = _parser.GenerateTemplate([]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));

        Assert.False(workbook.Worksheets.TryGetWorksheet("Categorías (no editar)", out _));
    }

    [Fact]
    public void Parse_ExtraeFilasConDatosYOmiteFilasVacias()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Corredores");
        sheet.Cell(1, 1).Value = "Nombre";
        sheet.Cell(2, 1).Value = "Ana";
        sheet.Cell(2, 2).Value = "Pérez";
        sheet.Cell(2, 3).Value = "101";
        sheet.Cell(2, 6).Value = "F";
        sheet.Cell(2, 8).Value = new DateTime(2011, 5, 15);
        sheet.Cell(2, 9).Value = "Juvenil";
        // fila 3 queda completamente vacía y debe omitirse
        sheet.Cell(4, 1).Value = "Beto";
        sheet.Cell(4, 3).Value = "102";
        sheet.Cell(4, 9).Value = "Infantil";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var rows = _parser.Parse(stream);

        Assert.Equal(2, rows.Count);

        var first = rows[0];
        Assert.Equal(2, first.Fila);
        Assert.Equal("Ana", first.Nombre);
        Assert.Equal("Pérez", first.Apellidos);
        Assert.Equal("101", first.Dorsal);
        Assert.Equal("F", first.Sexo);
        Assert.Equal(new DateTime(2011, 5, 15), first.FechaNacimiento);
        Assert.Equal(DateTimeKind.Utc, first.FechaNacimiento!.Value.Kind);
        Assert.Equal("Juvenil", first.Categoria);

        var second = rows[1];
        Assert.Equal(4, second.Fila);
        Assert.Equal("Beto", second.Nombre);
        Assert.Null(second.Apellidos);
        Assert.Null(second.FechaNacimiento);
    }

    [Fact]
    public void Parse_FechaComoTextoValido_SeInterpretaCorrectamente()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Corredores");
        sheet.Cell(1, 1).Value = "Nombre";
        sheet.Cell(2, 1).Value = "Ana";
        sheet.Cell(2, 3).Value = "101";
        sheet.Cell(2, 8).SetValue("2011-05-15");
        sheet.Cell(2, 9).Value = "Juvenil";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var rows = _parser.Parse(stream);

        Assert.Equal(new DateTime(2011, 5, 15), rows[0].FechaNacimiento);
        Assert.Equal(DateTimeKind.Utc, rows[0].FechaNacimiento!.Value.Kind);
    }

    [Fact]
    public void Parse_Fecha_SiempreTieneKindUtc_ParaQueNpgsqlNoRechaceElInsert()
    {
        // Regresión: Npgsql mapea DateTime a "timestamp with time zone" por default y
        // rechaza escribir un Kind distinto de Utc. ClosedXML devuelve Kind=Unspecified
        // para celdas de fecha nativas, lo que tumbaba el import completo con un 500 en
        // producción (Postgres) pese a que los tests con mocks no lo detectaban.
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Corredores");
        sheet.Cell(1, 1).Value = "Nombre";
        sheet.Cell(2, 1).Value = "Ana";
        sheet.Cell(2, 3).Value = "101";
        sheet.Cell(2, 8).Value = new DateTime(2011, 5, 15, 0, 0, 0, DateTimeKind.Unspecified);
        sheet.Cell(2, 9).Value = "Juvenil";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var rows = _parser.Parse(stream);

        Assert.Equal(DateTimeKind.Utc, rows[0].FechaNacimiento!.Value.Kind);
    }
}
