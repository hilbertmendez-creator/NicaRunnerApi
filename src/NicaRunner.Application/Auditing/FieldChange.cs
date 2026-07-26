namespace NicaRunner.Application.Auditing;

/// <summary>
/// Cambio de un campo (valor anterior → nuevo) candidato a auditarse. El
/// <see cref="IAuditService"/> descarta los que no cambiaron.
/// </summary>
public readonly record struct FieldChange(string Campo, string? ValorAnterior, string? ValorNuevo);
