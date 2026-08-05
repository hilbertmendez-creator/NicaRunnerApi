using ClosedXML.Excel;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Excel;

// public-runner-registration-manual-payment (design.md D12): modelado directamente sobre
// ExcelRunnerParser — mismas convenciones de ClosedXML — pero confirma Registrations
// existentes en vez de crear Runners desde cero. Solo se leen dos columnas
// (RegistrationId, Dorsal); el resto es referencia de solo lectura.
public class ExcelRegistrationParser : IExcelRegistrationParser
{
    private static readonly string[] Headers =
    [
        "RegistrationId", "Nombre", "Apellidos", "Categoría", "F. Nacimiento", "Referencia", "Dorsal"
    ];

    private static readonly string[] ReferenceHeaders =
    [
        "Código", "Nombre", "Distancia (km)", "Edad Mínima", "Edad Máxima"
    ];

    public List<ParsedConfirmRow> Parse(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<ParsedConfirmRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.IsEmpty())
                continue;

            // Solo se leen la columna 1 (RegistrationId, clave de join) y la columna 7
            // (Dorsal) — las columnas 2 a 6 son referencia de solo lectura para el admin,
            // nunca parseadas, así que editarlas por error nunca corrompe nada.
            var registrationId = ParseRegistrationId(row.Cell(1));
            var dorsal = row.Cell(7).GetString().Trim();

            rows.Add(new ParsedConfirmRow(rowNumber, registrationId, dorsal));
        }

        return rows;
    }

    public byte[] GenerateTemplate(IReadOnlyList<Registration> pendientes, IReadOnlyList<Category> referencia)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inscripciones");

        for (var i = 0; i < Headers.Length; i++)
            sheet.Cell(1, i + 1).Value = Headers[i];

        sheet.Row(1).Style.Font.Bold = true;

        for (var i = 0; i < pendientes.Count; i++)
        {
            var registration = pendientes[i];
            var rowNumber = i + 2;

            sheet.Cell(rowNumber, 1).Value = registration.Id;
            sheet.Cell(rowNumber, 2).Value = registration.Nombre;
            sheet.Cell(rowNumber, 3).Value = registration.Apellidos ?? string.Empty;
            sheet.Cell(rowNumber, 4).Value = registration.RaceCategory?.Category?.NombreCategoria ?? string.Empty;
            sheet.Cell(rowNumber, 5).Value = registration.FechaNacimiento;
            sheet.Cell(rowNumber, 6).Value = registration.ReferenciaTransferencia ?? string.Empty;
            // Columna 7 (Dorsal) queda en blanco a propósito — es el único input que el
            // admin llena antes de re-subir el archivo.
        }

        sheet.Column(5).Style.DateFormat.Format = "yyyy-mm-dd";
        sheet.Columns(1, Headers.Length).AdjustToContents();

        if (referencia.Count > 0)
        {
            // D12: hoja de referencia VISIBLE — lo OPUESTO al patrón de
            // ExcelRunnerParser.GenerateTemplate (hoja oculta usada como fuente de
            // validación de un dropdown). Acá no hay nada que elegir: la categoría ya
            // quedó fija desde la sumisión pública, así que esta hoja solo existe para que
            // el admin la lea como contexto mientras decide un dorsal.
            var referenceSheet = workbook.Worksheets.Add("Categorías (referencia)");

            for (var i = 0; i < ReferenceHeaders.Length; i++)
                referenceSheet.Cell(1, i + 1).Value = ReferenceHeaders[i];

            referenceSheet.Row(1).Style.Font.Bold = true;

            for (var i = 0; i < referencia.Count; i++)
            {
                var category = referencia[i];
                var rowNumber = i + 2;

                referenceSheet.Cell(rowNumber, 1).Value = category.Codigo;
                referenceSheet.Cell(rowNumber, 2).Value = category.NombreCategoria;
                referenceSheet.Cell(rowNumber, 3).Value = category.Distancia;
                referenceSheet.Cell(rowNumber, 4).Value = category.EdadMinima;
                referenceSheet.Cell(rowNumber, 5).Value = category.EdadMaxima;
            }

            referenceSheet.Columns(1, ReferenceHeaders.Length).AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Mismo idioma dual (numérico primero, texto como fallback) que ExcelRunnerParser.ParseFecha
    // usa para fechas — acá aplicado a un entero, no a una fecha.
    private static int? ParseRegistrationId(IXLCell cell)
    {
        if (cell.TryGetValue(out int id))
            return id;

        return int.TryParse(cell.GetString().Trim(), out var parsed) ? parsed : null;
    }
}
