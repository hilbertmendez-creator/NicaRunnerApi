using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NicaRunner.Infrastructure.Notifications;

namespace NicaRunner.Tests;

public class ResendEmailSenderTests
{
    [Theory]
    [InlineData("", "test@test.com")]
    [InlineData("re_test", "")]
    public async Task SendAsync_ConfiguracionIncompleta_FallaSinLlamarAResend(
        string apiKey,
        string fromEmail)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEmailSender(client, Options.Create(new ResendOptions
        {
            ApiKey = apiKey,
            FromEmail = fromEmail
        }));

        var result = await sender.SendAsync("dest@test.com", "texto");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        // MockBehavior.Strict: cualquier request HTTP habría lanzado.
        handler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithHtml_IncludesBothFieldsInPayload()
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
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEmailSender(client, Options.Create(new ResendOptions
        {
            ApiKey = "re_test",
            FromEmail = "test@test.com",
            Subject = "Default"
        }));

        await sender.SendAsync("dest@test.com", "plain text", "Subject", "<p>html</p>");

        Assert.NotNull(capturedJson);
        using var doc = JsonDocument.Parse(capturedJson!);
        Assert.Equal("plain text", doc.RootElement.GetProperty("text").GetString());
        Assert.Equal("<p>html</p>", doc.RootElement.GetProperty("html").GetString());
    }

    [Fact]
    public async Task SendAsync_WithoutHtml_SendsTextOnly()
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
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEmailSender(client, Options.Create(new ResendOptions
        {
            ApiKey = "re_test",
            FromEmail = "test@test.com"
        }));

        await sender.SendAsync("dest@test.com", "solo text");

        using var doc = JsonDocument.Parse(capturedJson!);
        Assert.True(doc.RootElement.TryGetProperty("text", out _));
        Assert.False(doc.RootElement.TryGetProperty("html", out _));
    }
}
