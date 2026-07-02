using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Notifications;

/// <summary>
/// Envía email real vía la API de Resend (https://resend.com). Requiere
/// "Resend:ApiKey" configurado (appsettings, user-secrets o variable de entorno
/// RESEND__APIKEY) — sin eso, falla honestamente igual que el stub anterior.
/// </summary>
public class ResendEmailSender(HttpClient httpClient, IOptions<ResendOptions> options) : INotificationSender
{
    private readonly ResendOptions _options = options.Value;

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationSendResult> SendAsync(string destino, string mensaje, string? subject = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return new NotificationSendResult(
                false,
                "Falta configurar Resend:ApiKey (ver appsettings.Development.json o user-secrets).");
        }

        var payload = new
        {
            from = _options.FromEmail,
            to = new[] { destino },
            subject = subject ?? _options.Subject,
            text = mensaje,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
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
            var detail = ExtractErrorMessage(body) ?? $"Resend respondió {(int)response.StatusCode}.";
            return new NotificationSendResult(false, detail);
        }
        catch (HttpRequestException ex)
        {
            return new NotificationSendResult(false, $"No se pudo contactar a Resend: {ex.Message}");
        }
    }

    private static string? ExtractErrorMessage(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.TryGetProperty("message", out var message) ? message.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
