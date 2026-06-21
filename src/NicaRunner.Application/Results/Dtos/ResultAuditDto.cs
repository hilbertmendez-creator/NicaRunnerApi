namespace NicaRunner.Application.Results.Dtos;

public record ResultAuditDto(
    int Id,
    int ResultId,
    int AdminId,
    string CampoModificado,
    string ValorAnterior,
    string ValorNuevo,
    string? Razon,
    DateTime CreatedAt);
