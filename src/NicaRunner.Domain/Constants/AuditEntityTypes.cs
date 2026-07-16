namespace NicaRunner.Domain.Constants;

/// <summary>
/// Discriminadores de <see cref="Entities.AuditLog"/>. Constantes (no enum) para no
/// acoplar el dominio a nombres de pantalla y mantener consultas SQL legibles.
/// </summary>
public static class AuditEntityTypes
{
    public const string User = "User";
    public const string Race = "Race";
    public const string Category = "Category";
}
