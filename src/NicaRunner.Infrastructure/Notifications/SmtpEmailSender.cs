using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Notifications;

/// <summary>
/// Envía email real vía SMTP (pensado para una cuenta de Gmail con App
/// Password, ver docs/render-setup.md). Requiere "Smtp:User", "Smtp:Password"
/// y "Smtp:FromEmail" configurados — sin eso, falla honestamente igual que el
/// stub anterior. Gmail personal limita a ~500 envíos/día; si eso se vuelve
/// una restricción real, migrar a un proveedor transaccional con dominio
/// propio verificado (Resend, SES, etc.).
/// </summary>
public class SmtpEmailSender(IOptions<SmtpOptions> options) : INotificationSender
{
    private readonly SmtpOptions _options = options.Value;

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationSendResult> SendAsync(
        string destino,
        string mensaje,
        string? subject = null,
        string? html = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.User) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            return new NotificationSendResult(
                false,
                "Falta configurar Smtp:User, Smtp:Password o Smtp:FromEmail (ver docs/render-setup.md).");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.FromEmail));
        message.To.Add(MailboxAddress.Parse(destino));
        message.Subject = subject ?? _options.Subject;

        var body = new BodyBuilder { TextBody = mensaje };
        if (!string.IsNullOrWhiteSpace(html))
            body.HtmlBody = html;
        message.Body = body.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_options.User, _options.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            return new NotificationSendResult(true, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new NotificationSendResult(false, $"No se pudo enviar vía SMTP: {ex.Message}");
        }
    }
}
