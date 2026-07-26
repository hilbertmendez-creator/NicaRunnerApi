namespace NicaRunner.Application.Auditing.Dtos;

public record AuditLogDto(
    int Id,
    string EntityType,
    int EntityId,
    string Campo,
    string? ValorAnterior,
    string? ValorNuevo,
    int AutorId,
    string AutorNombre,
    DateTime CreatedAt);
