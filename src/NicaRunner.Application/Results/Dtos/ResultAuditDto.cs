namespace NicaRunner.Application.Results.Dtos;

public record ResultAuditDto(
    int Id,
    int ResultId,
    int ActorUserId,
    string CampoModificado,
    string ValorAnterior,
    string ValorNuevo,
    string? Razon,
    DateTime CreatedAt);
