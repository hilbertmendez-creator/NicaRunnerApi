namespace NicaRunner.Application.Notifications.Dtos;

public record NotifyAllSummaryDto(
    int TotalResultados,
    int NotificacionesCreadas,
    int Enviadas,
    int Fallidas);
