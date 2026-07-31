namespace NicaRunner.Infrastructure.Notifications;

public class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "NicaRunner";
    public string Subject { get; set; } = "Tu resultado en NicaRunner";
}
