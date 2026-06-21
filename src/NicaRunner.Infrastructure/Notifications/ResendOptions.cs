namespace NicaRunner.Infrastructure.Notifications;

public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "NicaRunner <onboarding@resend.dev>";
    public string Subject { get; set; } = "Tu resultado en NicaRunner";
}
