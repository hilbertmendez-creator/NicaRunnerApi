namespace NicaRunner.Application.Notifications.EmailTemplates;

public record WelcomeAccountEmailModel(
    string RecipientName,
    string TempPassword,
    string ProductName = "NicaRunner Backoffice");
