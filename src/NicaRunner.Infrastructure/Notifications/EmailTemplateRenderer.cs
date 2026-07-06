using System.Text;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications.EmailTemplates;

namespace NicaRunner.Infrastructure.Notifications;

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private const string ResourcePrefix = "NicaRunner.Infrastructure.Notifications.Templates.";

    private readonly string _raceResult;
    private readonly string _passwordReset;
    private readonly string _welcomeAccount;

    public EmailTemplateRenderer()
    {
        _raceResult = LoadTemplate("RaceResult.html");
        _passwordReset = LoadTemplate("PasswordReset.html");
        _welcomeAccount = LoadTemplate("WelcomeAccount.html");
    }

    public RenderedEmail RenderRaceResult(RaceResultEmailModel model) =>
        new(
            WrapPartial(ApplyDesignTokens(_raceResult, new Dictionary<string, string>
            {
                ["recipient_name"] = E(model.RecipientName),
                ["race_name"] = E(model.RaceName),
                ["position"] = model.Position.ToString(),
                ["arrival_time"] = FormatArrivalTime(model.ArrivalTime),
            })),
            BuildRaceResultText(model),
            "Tu resultado en NicaRunner");

    public RenderedEmail RenderPasswordReset(PasswordResetEmailModel model)
    {
        if (!IsValidHttpUrl(model.ResetUrl))
            throw new TemplateRenderException("ResetUrl inválida.");

        return new(
            WrapPartial(ApplyDesignTokens(_passwordReset, new Dictionary<string, string>
            {
                ["recipient_name"] = E(model.RecipientName),
                ["reset_url"] = E(model.ResetUrl),
                ["expires_minutes"] = model.ExpiresMinutes.ToString(),
            })),
            BuildPasswordResetText(model),
            "Restablece tu contraseña de NicaRunner");
    }

    public RenderedEmail RenderWelcomeAccount(WelcomeAccountEmailModel model) =>
        new(
            WrapPartial(ApplyDesignTokens(_welcomeAccount, new Dictionary<string, string>
            {
                ["recipient_name"] = E(model.RecipientName),
                ["product_name"] = E(model.ProductName),
                ["temp_password"] = E(model.TempPassword),
            }), model.ProductName),
            BuildWelcomeAccountText(model),
            "Tu cuenta en NicaRunner Backoffice");

    private static string WrapPartial(string bodyHtml, string productName = "NicaRunner") =>
        EmailLayoutBuilder.Wrap(bodyHtml, productName, "Gestión de competencias de atletismo");

    private static string ApplyDesignTokens(string template, Dictionary<string, string> values)
    {
        var result = template
            .Replace("{{ bg_app }}", EmailDesignTokens.BgApp)
            .Replace("{{ bg_card }}", EmailDesignTokens.BgCard)
            .Replace("{{ header_from }}", EmailDesignTokens.HeaderFrom)
            .Replace("{{ header_to }}", EmailDesignTokens.HeaderTo)
            .Replace("{{ border_card }}", EmailDesignTokens.BorderCard)
            .Replace("{{ text_hi }}", EmailDesignTokens.TextHi)
            .Replace("{{ text_lo }}", EmailDesignTokens.TextLo)
            .Replace("{{ text_xs }}", EmailDesignTokens.TextXs)
            .Replace("{{ accent_bg }}", EmailDesignTokens.AccentBg)
            .Replace("{{ accent_border }}", EmailDesignTokens.AccentBorder)
            .Replace("{{ accent_text }}", EmailDesignTokens.AccentText)
            .Replace("{{ cta_from }}", EmailDesignTokens.CtaFrom)
            .Replace("{{ cta_to }}", EmailDesignTokens.CtaTo)
            .Replace("{{ font_sans }}", EmailDesignTokens.FontSans)
            .Replace("{{ font_mono }}", EmailDesignTokens.FontMono)
            .Replace("{{ radius_card }}", EmailDesignTokens.RadiusCard.ToString())
            .Replace("{{ radius_btn }}", EmailDesignTokens.RadiusBtn.ToString());

        foreach (var (key, value) in values)
            result = result.Replace($"{{{{ {key} }}}}", value);

        return result;
    }

    // Escapa solo caracteres peligrosos — preserva acentos UTF-8 en clientes modernos
    private static string E(string value) =>
        string.IsNullOrEmpty(value)
            ? ""
            : value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");

    private static string BuildRaceResultText(RaceResultEmailModel model) =>
        $"Hola {model.RecipientName}, tu resultado en {model.RaceName} fue: posición {model.Position}, " +
        $"tiempo {FormatArrivalTime(model.ArrivalTime)}. ¡Gracias por participar!";

    private static string BuildPasswordResetText(PasswordResetEmailModel model) =>
        new StringBuilder()
            .AppendLine($"Hola {model.RecipientName}, recibimos una solicitud para restablecer tu contraseña de NicaRunner Backoffice.")
            .AppendLine($"Este link es válido por {model.ExpiresMinutes} minutos:")
            .AppendLine(model.ResetUrl)
            .AppendLine()
            .Append("Si no solicitaste esto, ignora este correo.")
            .ToString();

    private static string BuildWelcomeAccountText(WelcomeAccountEmailModel model) =>
        new StringBuilder()
            .AppendLine($"Hola {model.RecipientName}, se creó tu cuenta en {model.ProductName}.")
            .AppendLine($"Tu contraseña temporal es: {model.TempPassword}")
            .AppendLine()
            .Append("Deberás cambiarla al iniciar sesión por primera vez.")
            .ToString();

    private static string FormatArrivalTime(DateTime arrivalTime) =>
        arrivalTime.ToString("HH:mm:ss");

    private static bool IsValidHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string LoadTemplate(string fileName)
    {
        var assembly = typeof(EmailTemplateRenderer).Assembly;
        var resourceName = $"{ResourcePrefix}{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new TemplateRenderException($"Plantilla no encontrada: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
