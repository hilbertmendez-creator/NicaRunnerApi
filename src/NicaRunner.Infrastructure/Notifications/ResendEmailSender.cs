using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Notifications;

/// <summary>
/// Envía email real vía la API de Resend (https://resend.com). Requiere
/// "Resend:ApiKey" y "Resend:FromEmail" configurados (appsettings, user-secrets
/// o variables de entorno <c>Resend__ApiKey</c> / <c>Resend__FromEmail</c>) — sin
/// eso falla honestamente, sin intentar el envío. El remitente debe pertenecer a
/// un dominio verificado en resend.com/domains; con uno sin verificar Resend
/// responde 403 y solo entrega al dueño de la cuenta. Ver docs/render-setup.md.
/// </summary>
public class ResendEmailSender(HttpClient httpClient, IOptions<ResendOptions> options) : INotificationSender
{
    private readonly ResendOptions _options = options.Value;

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationSendResult> SendAsync(
        string destino,
        string mensaje,
        string? subject = null,
        string? html = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return new NotificationSendResult(
                false,
                "Falta configurar Resend:ApiKey (ver appsettings.Development.json o user-secrets).");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            return new NotificationSendResult(
                false,
                "Falta configurar Resend:FromEmail con una dirección de un dominio verificado en resend.com/domains.");
        }

        var payload = new Dictionary<string, object>
        {
            ["from"] = _options.FromEmail,
            ["to"] = new[] { destino },
            ["subject"] = subject ?? _options.Subject,
            ["text"] = mensaje,
        };

        if (!string.IsNullOrWhiteSpace(html))
            payload["html"] = html;

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
