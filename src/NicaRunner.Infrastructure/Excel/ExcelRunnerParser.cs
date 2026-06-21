using ClosedXML.Excel;
using NicaRunner.Application.Common.Interfaces;

namespace NicaRunner.Infrastructure.Excel;

public class ExcelRunnerParser : IExcelRunnerParser
{
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
            var dorsal = row.Cell(2).GetString().Trim();
            var telefono = NullIfEmpty(row.Cell(3).GetString().Trim());
            var email = NullIfEmpty(row.Cell(4).GetString().Trim());
            var edad = ParseEdad(row.Cell(5));
            var categoria = row.Cell(6).GetString().Trim();

            rows.Add(new ParsedRunnerRow(rowNumber, nombre, dorsal, telefono, email, edad, categoria));
        }

        return rows;
    }

    private static int? ParseEdad(IXLCell cell)
    {
        if (cell.TryGetValue(out int edad))
            return edad;

        return int.TryParse(cell.GetString().Trim(), out var parsed) ? parsed : null;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
