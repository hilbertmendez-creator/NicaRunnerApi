using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NicaRunner.Infrastructure.Notifications;

namespace NicaRunner.Tests;

public class SendGridEmailSenderTests
{
    private static SendGridEmailSender BuildSender(HttpMessageHandler handler, SendGridOptions? options = null)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.sendgrid.com/") };
        return new SendGridEmailSender(client, Options.Create(options ?? new SendGridOptions
        {
            ApiKey = "sg_test",
            FromEmail = "from@test.com",
            FromName = "NicaRunner"
        }));
    }

    [Fact]
    public async Task SendAsync_WithHtml_IncludesBothContentTypesInPayload()
    {
        string? capturedJson = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedJson = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            });

        var sender = BuildSender(handler.Object);

        var result = await sender.SendAsync("dest@test.com", "plain text", "Subject", "<p>html</p>");

        Assert.True(result.Success);
        Assert.NotNull(capturedJson);
        using var doc = JsonDocument.Parse(capturedJson!);
        var content = doc.RootElement.GetProperty("content");
        Assert.Equal(2, content.GetArrayLength());
        Assert.Equal("text/plain", content[0].GetProperty("type").GetString());
        Assert.Equal("text/html", content[1].GetProperty("type").GetString());
    }

    [Fact]
    public async Task SendAsync_WithoutHtml_SendsPlainTextOnly()
    {
        string? capturedJson = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedJson = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            });

        var sender = BuildSender(handler.Object);

        await sender.SendAsync("dest@test.com", "solo text");

        using var doc = JsonDocument.Parse(capturedJson!);
        Assert.Equal(1, doc.RootElement.GetProperty("content").GetArrayLength());
    }

    [Fact]
    public async Task SendAsync_WithSendGridError_ReturnsFailureWithMessage()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("""{"errors":[{"message":"The from address does not match a verified Sender Identity"}]}""")
            });

        var sender = BuildSender(handler.Object);

        var result = await sender.SendAsync("dest@test.com", "texto");

        Assert.False(result.Success);
        Assert.Equal("The from address does not match a verified Sender Identity", result.ErrorMessage);
    }

    [Fact]
    public async Task SendAsync_WithMissingConfig_ReturnsFailureWithoutCallingHttp()
    {
        var handler = new Mock<HttpMessageHandler>();
        var sender = BuildSender(handler.Object, new SendGridOptions { ApiKey = "", FromEmail = "" });

        var result = await sender.SendAsync("dest@test.com", "texto");

        Assert.False(result.Success);
        Assert.Contains("SendGrid:", result.ErrorMessage);
        handler.Protected().Verify(
            "SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}
