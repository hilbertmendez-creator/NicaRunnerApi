namespace NicaRunner.Application.AdminNotifications.Dtos;

public record AdminNotificationDto(int Id, string Type, string Mensaje, int? RaceId, DateTime CreatedAt, bool Leida);

public record AdminNotificationsPageDto(List<AdminNotificationDto> Items, int UnreadCount);
