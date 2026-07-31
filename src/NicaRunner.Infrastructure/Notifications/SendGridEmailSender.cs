using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Notifications;

/// <summary>
/// Envía email real vía la API de SendGrid (https://sendgrid.com), usando un
/// remitente verificado con Single Sender Verification (no requiere dominio
/// propio — ver docs/render-setup.md). Requiere "SendGrid:ApiKey" configurado
/// — sin eso, falla honestamente igual que el stub anterior.
/// </summary>
public class SendGridEmailSender(HttpClient httpClient, IOptions<SendGridOptions> options) : INotificationSender
{
    private readonly SendGridOptions _options = options.Value;

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationSendResult> SendAsync(
        string destino,
        string mensaje,
        string? subject = null,
        string? html = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            return new NotificationSendResult(
                false,
                "Falta configurar SendGrid:ApiKey o SendGrid:FromEmail (ver docs/render-setup.md).");
        }

        var content = new List<Dictionary<string, string>>
        {
            new() { ["type"] = "text/plain", ["value"] = mensaje }
        };
        if (!string.IsNullOrWhiteSpace(html))
            content.Add(new Dictionary<string, string> { ["type"] = "text/html", ["value"] = html });

        var payload = new Dictionary<string, object>
        {
            ["personalizations"] = new[] { new Dictionary<string, object> { ["to"] = new[] { new { email = destino } } } },
            ["from"] = new { email = _options.FromEmail, name = _options.FromName },
            ["subject"] = subject ?? _options.Subject,
            ["content"] = content
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/mail/send")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return new NotificationSendResult(true, null);

            var body = await response.Content.ReadAsStringAsync(ct);
            var detail = ExtractErrorMessage(body) ?? $"SendGrid respondió {(int)response.StatusCode}.";
            return new NotificationSendResult(false, detail);
        }
        catch (HttpRequestException ex)
        {
            return new NotificationSendResult(false, $"No se pudo contactar a SendGrid: {ex.Message}");
        }
    }

    private static string? ExtractErrorMessage(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                return errors[0].TryGetProperty("message", out var message) ? message.GetString() : null;
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
