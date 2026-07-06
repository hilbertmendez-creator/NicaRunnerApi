namespace NicaRunner.Application.Notifications.EmailTemplates;

public record RaceResultEmailModel(
    string RecipientName,
    string RaceName,
    int Position,
    DateTime ArrivalTime);
