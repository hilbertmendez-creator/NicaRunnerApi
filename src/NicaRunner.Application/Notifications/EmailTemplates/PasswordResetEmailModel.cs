namespace NicaRunner.Application.Notifications.EmailTemplates;

public record PasswordResetEmailModel(
    string RecipientName,
    string ResetUrl,
    int ExpiresMinutes = 30);
