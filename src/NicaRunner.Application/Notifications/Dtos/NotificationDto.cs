using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Notifications.Dtos;

public record NotificationDto(
    int Id,
    int RaceId,
    int RunnerId,
    int ResultId,
    NotificationChannel Channel,
    NotificationStatus Status,
    string Mensaje,
    string? Error,
    DateTime CreatedAt,
    DateTime? SentAt);
