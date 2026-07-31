using Microsoft.Extensions.Options;
using NicaRunner.Infrastructure.Notifications;

namespace NicaRunner.Tests;

public class SmtpEmailSenderTests
{
    [Theory]
    [InlineData("", "app-password", "from@test.com")]
    [InlineData("user@test.com", "", "from@test.com")]
    [InlineData("user@test.com", "app-password", "")]
    public async Task SendAsync_WithMissingConfig_ReturnsFailureWithoutConnecting(
        string user, string password, string fromEmail)
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions
        {
            User = user,
            Password = password,
            FromEmail = fromEmail
        }));

        var result = await sender.SendAsync("dest@test.com", "mensaje");

        Assert.False(result.Success);
        Assert.Contains("Smtp:", result.ErrorMessage);
    }
}
