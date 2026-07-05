using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public record ParsedRunnerRow(
    int Fila,
    string Nombre,
    string? Apellidos,
    string Dorsal,
    string? Telefono,
    string? Email,
    string? Sexo,
    string? Club,
    DateTime? FechaNacimiento,
    string Categoria);

public interface IExcelRunnerParser
{
    List<ParsedRunnerRow> Parse(Stream excelStream);

    // Genera la plantilla .xlsx con la lista de categorías de la carrera como
    // dropdown validado, para que el corredor solo pueda elegir una categoría existente.
    byte[] GenerateTemplate(IReadOnlyList<Category> categories);
}
