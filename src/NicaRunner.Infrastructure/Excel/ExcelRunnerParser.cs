using ClosedXML.Excel;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Excel;

public class ExcelRunnerParser : IExcelRunnerParser
{
    private static readonly string[] Headers =
    [
        "Nombre", "Apellidos", "Dorsal", "Teléfono", "Email", "Sexo (M/F)", "Club", "Fecha de Nacimiento", "Categoría"
    ];

    private const int TemplateDataRows = 500;

    public List<ParsedRunnerRow> Parse(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<ParsedRunnerRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.IsEmpty())
                continue;

            var nombre = row.Cell(1).GetString().Trim();
            var apellidos = NullIfEmpty(row.Cell(2).GetString().Trim());
            var dorsal = row.Cell(3).GetString().Trim();
            var telefono = NullIfEmpty(row.Cell(4).GetString().Trim());
            var email = NullIfEmpty(row.Cell(5).GetString().Trim());
            var sexo = NullIfEmpty(row.Cell(6).GetString().Trim());
            var club = NullIfEmpty(row.Cell(7).GetString().Trim());
            var fechaNacimiento = ParseFecha(row.Cell(8));
            var categoria = row.Cell(9).GetString().Trim();

            rows.Add(new ParsedRunnerRow(rowNumber, nombre, apellidos, dorsal, telefono, email, sexo, club, fechaNacimiento, categoria));
        }

        return rows;
    }

    public byte[] GenerateTemplate(IReadOnlyList<Category> categories)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Corredores");

        for (var i = 0; i < Headers.Length; i++)
            sheet.Cell(1, i + 1).Value = Headers[i];

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns(1, Headers.Length).AdjustToContents();

        var fechaColumn = sheet.Column(8);
        fechaColumn.Style.DateFormat.Format = "yyyy-mm-dd";

        var sexoRange = sheet.Range(2, 6, TemplateDataRows, 6);
        sexoRange.CreateDataValidation().List("M,F");

        if (categories.Count > 0)
        {
            var lookupSheet = workbook.Worksheets.Add("Categorías (no editar)");
            for (var i = 0; i < categories.Count; i++)
                lookupSheet.Cell(i + 1, 1).Value = categories[i].NombreCategoria;
            lookupSheet.Hide();

            var categoriaRange = sheet.Range(2, 9, TemplateDataRows, 9);
            categoriaRange.CreateDataValidation().List(lookupSheet.Range(1, 1, categories.Count, 1));
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Kind explícito a Utc: ClosedXML (y DateTime.TryParse de un string plano) devuelven
    // DateTime con Kind=Unspecified, pero Npgsql mapea DateTime a "timestamp with time
    // zone" por default y rechaza escribir un Kind distinto de Utc ("Cannot write
    // DateTime with Kind=Unspecified..."). El alta manual no lo sufre porque el frontend
    // manda fechas ISO con "Z" (Kind=Utc tras deserializar); la importación por Excel sí,
    // y sin este fix cada fila con fecha de nacimiento revienta el import con 500.
    private static DateTime? ParseFecha(IXLCell cell)
    {
        if (cell.TryGetValue(out DateTime fecha))
            return DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

        return DateTime.TryParse(cell.GetString().Trim(), out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
