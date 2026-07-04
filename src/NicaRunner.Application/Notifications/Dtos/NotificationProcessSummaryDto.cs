namespace NicaRunner.Application.Notifications.Dtos;

public record NotificationProcessSummaryDto(
    int Procesadas,
    int Enviadas,
    int Fallidas);
