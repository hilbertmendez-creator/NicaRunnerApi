using Microsoft.Extensions.Options;
using NicaRunner.Application.Common;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications.EmailTemplates;

namespace NicaRunner.Api.Dev;

/// <summary>
/// Preview de plantillas de correo — solo disponible en Development.
/// </summary>
public static class EmailPreviewEndpoints
{
    private const string SampleName = "María López";
    private const string SampleRace = "10K Managua";
    private static readonly DateTime SampleArrival = new(2026, 6, 29, 1, 23, 45, DateTimeKind.Utc);

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/dev/emails")
            .WithTags("Email Preview (Dev)")
            .ExcludeFromDescription();

        group.MapGet("/", () => Results.Content(BuildIndexHtml(), "text/html; charset=utf-8"));

        group.MapGet("/race-result", (IEmailTemplateRenderer renderer, string? format) =>
            RenderResponse(renderer.RenderRaceResult(new RaceResultEmailModel(
                SampleName, SampleRace, 3, SampleArrival)), format));

        group.MapGet("/password-reset", (IEmailTemplateRenderer renderer, IOptions<FrontendOptions> frontend, string? format) =>
        {
            var baseUrl = string.IsNullOrWhiteSpace(frontend.Value.BaseUrl)
                ? "http://localhost:5173"
                : frontend.Value.BaseUrl;
            var resetUrl = EmailLinkBuilder.BuildResetUrl(baseUrl, "preview-token-abc123");
            return RenderResponse(renderer.RenderPasswordReset(new PasswordResetEmailModel(SampleName, resetUrl)), format);
        });

        group.MapGet("/welcome-account", (IEmailTemplateRenderer renderer, string? format) =>
            RenderResponse(renderer.RenderWelcomeAccount(new WelcomeAccountEmailModel(
                SampleName, "Temp#Pass1!")), format));
    }

    private static IResult RenderResponse(RenderedEmail rendered, string? format) =>
        string.Equals(format, "text", StringComparison.OrdinalIgnoreCase)
            ? Results.Text(rendered.Text, "text/plain; charset=utf-8")
            : Results.Content(rendered.Html, "text/html; charset=utf-8");

    private static string BuildIndexHtml() =>
        """
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>NicaRunner — Email Preview (Dev)</title>
          <style>
            body { font-family: system-ui, sans-serif; max-width: 640px; margin: 48px auto; padding: 0 16px; color: #11243F; background: #F5F9FF; }
            h1 { font-size: 1.5rem; margin-bottom: 0.25rem; }
            p.sub { color: #5B6B82; font-size: 0.9rem; margin-top: 0; }
            ul { list-style: none; padding: 0; }
            li { margin: 12px 0; }
            a.card, div.card { display: block; padding: 16px 20px; background: #fff; border: 1px solid #D6EAFF; border-radius: 14px; text-decoration: none; color: #11243F; }
            a.card:hover, div.card:hover { border-color: #1565FF; }
            div.card a { text-decoration: none; color: #11243F; }
            div.card a:hover { color: #1565FF; }
            a.card strong { color: #0D47A1; }
            .meta { font-size: 0.8rem; color: #9AA5B1; margin-top: 6px; }
            .warn { background: #EEF3FF; border: 1px solid #BFDBFE; border-radius: 10px; padding: 12px 16px; font-size: 0.85rem; color: #1D4ED8; margin-bottom: 24px; }
          </style>
        </head>
        <body>
          <h1>Email Preview</h1>
          <p class="sub">Solo disponible en <strong>Development</strong> — no se envía correo real.</p>
          <div class="warn">Estos endpoints no existen en Production.</div>
          <ul>
            <li>
              <div class="card">
                <a href="/dev/emails/race-result" target="_blank"><strong>EM-01</strong> — Resultado de carrera</a>
                <div class="meta">HTML · <a href="/dev/emails/race-result?format=text">text/plain</a></div>
              </div>
            </li>
            <li>
              <div class="card">
                <a href="/dev/emails/password-reset" target="_blank"><strong>EM-02</strong> — Reset de contraseña</a>
                <div class="meta">HTML · <a href="/dev/emails/password-reset?format=text">text/plain</a></div>
              </div>
            </li>
            <li>
              <div class="card">
                <a href="/dev/emails/welcome-account" target="_blank"><strong>EM-03</strong> — Cuenta nueva backoffice</a>
                <div class="meta">HTML · <a href="/dev/emails/welcome-account?format=text">text/plain</a></div>
              </div>
            </li>
          </ul>
        </body>
        </html>
        """;
}
