namespace NicaRunner.Application.Common.Interfaces;

public record ParsedRunnerRow(
    int Fila,
    string Nombre,
    string Dorsal,
    string? Telefono,
    string? Email,
    int? Edad,
    string Categoria);

public interface IExcelRunnerParser
{
    List<ParsedRunnerRow> Parse(Stream excelStream);
}
