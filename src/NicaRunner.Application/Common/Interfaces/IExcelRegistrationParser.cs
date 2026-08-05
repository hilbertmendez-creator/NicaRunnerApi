using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

// public-runner-registration-manual-payment (design.md D12): parser análogo a
// IExcelRunnerParser/ExcelRunnerParser, pero para CONFIRMAR Registrations existentes en
// bloque en vez de crear Runners desde cero. Solo se leen dos columnas — RegistrationId
// (clave de join) y Dorsal — el resto de columnas de la plantilla (Nombre, Apellidos,
// Categoría, F. Nacimiento, Referencia) son datos de referencia de solo lectura para el
// admin: decoración, no input parseado, así que editarlas por error nunca corrompe nada.
public record ParsedConfirmRow(int Fila, int? RegistrationId, string Dorsal);

public interface IExcelRegistrationParser
{
    // RegistrationId en blanco o no parseable es un error de fila (RegistrationId=null),
    // NUNCA un salto silencioso — el caller decide qué hacer con la fila.
    List<ParsedConfirmRow> Parse(Stream excelStream);

    // Exporta las Registrations en estado ComprobanteSubido de esta carrera (una fila cada
    // una, columna Dorsal en blanco para que el admin la llene) más una segunda hoja de
    // referencia con el catálogo de categorías de la carrera — VISIBLE, no oculta ni
    // fuente de validación de dropdown, a diferencia de
    // ExcelRunnerParser.GenerateTemplate (D12: la categoría ya viene fija desde la
    // sumisión pública, no hay nada que elegir — el admin solo la lee como contexto).
    byte[] GenerateTemplate(IReadOnlyList<Registration> pendientes, IReadOnlyList<Category> referencia);
}
