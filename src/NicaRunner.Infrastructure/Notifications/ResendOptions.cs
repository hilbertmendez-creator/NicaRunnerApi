namespace NicaRunner.Infrastructure.Notifications;

public class ResendOptions
{
    /// <summary>
    /// Llave de resend.com/api-keys, scope "Send only". Variable de entorno
    /// <c>Resend__ApiKey</c>. Ver docs/render-setup.md.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Remitente de todos los correos, formato <c>Nombre &lt;buzon@dominio&gt;</c>.
    /// Tiene que pertenecer a un dominio verificado en resend.com/domains: Resend
    /// rechaza con 403 cualquier envío a terceros desde un remitente no verificado.
    /// Sin valor no se envía nada — a propósito, para que una configuración
    /// faltante falle de inmediato en vez de descubrirse recién en el 403.
    /// Variable de entorno <c>Resend__FromEmail</c>. Ver docs/render-setup.md.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    public string Subject { get; set; } = "Tu resultado en NicaRunner";
}
