namespace NicaRunner.Domain.Entities;

/// <summary>
/// Bitácora de auditoría transversal: una fila por campo modificado en una entidad
/// del backoffice (Usuario, Carrera, Categoría). Append-only (inmutable).
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;   // "User" | "Race" | "Category"
    public int EntityId { get; set; }
    public string Campo { get; set; } = string.Empty;         // "Nombre", "Role", "IsActive", "Distancia", ...
    public string? ValorAnterior { get; set; }                // null = campo vacío real (no "")
    public string? ValorNuevo { get; set; }
    public int AutorId { get; set; }                          // usuario que modificó (tomado del claim)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // UTC; se muestra en hora local

    public User Autor { get; set; } = null!;                  // solo para proyección en lectura
}
