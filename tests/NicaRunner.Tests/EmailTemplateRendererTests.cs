using NicaRunner.Application.Notifications.EmailTemplates;
using NicaRunner.Infrastructure.Notifications;

namespace NicaRunner.Tests;

public class EmailTemplateRendererTests
{
    private readonly EmailTemplateRenderer _renderer = new();

    [Fact]
    public void RaceResult_RendersHtmlWithBrandColorsAndPlainText()
    {
        var model = new RaceResultEmailModel("María López", "10K Managua", 3,
            new DateTime(2026, 6, 29, 1, 23, 45, DateTimeKind.Utc));

        var result = _renderer.RenderRaceResult(model);

        Assert.Contains("#0D47A1", result.Html);
        Assert.Contains("María López", result.Html);
        Assert.Contains("#3", result.Html);
        Assert.Contains("01:23:45", result.Html);
        Assert.Equal("Tu resultado en NicaRunner", result.Subject);
        Assert.Contains("María López", result.Text);
        Assert.Contains("posición 3", result.Text);
        Assert.Contains("01:23:45", result.Text);
    }

    [Fact]
    public void RaceResult_EscapesXssInRecipientName()
    {
        var model = new RaceResultEmailModel("<script>alert(1)</script>", "Carrera", 1,
            new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));

        var result = _renderer.RenderRaceResult(model);

        Assert.Contains("&lt;script&gt;", result.Html);
        Assert.Contains("<script>alert(1)</script>", result.Text);
    }

    [Fact]
    public void PasswordReset_IncludesCtaAndResetUrl()
    {
        var model = new PasswordResetEmailModel("Ana", "https://app.test/reset-password?token=abc123", 30);

        var result = _renderer.RenderPasswordReset(model);

        Assert.Contains("Restablecer contraseña", result.Html);
        Assert.Contains("https://app.test/reset-password?token=abc123", result.Html);
        Assert.Contains("30 minutos", result.Html);
        Assert.Contains("https://app.test/reset-password?token=abc123", result.Text);
    }

    [Fact]
    public void WelcomeAccount_ShowsTempPasswordInMonospaceBlock()
    {
        var model = new WelcomeAccountEmailModel("Carlos", "Temp#Pass1!");

        var result = _renderer.RenderWelcomeAccount(model);

        Assert.Contains("Temp#Pass1!", result.Html);
        Assert.Contains("NicaRunner Backoffice", result.Html);
        Assert.Contains("Temp#Pass1!", result.Text);
        Assert.Equal("Tu cuenta en NicaRunner Backoffice", result.Subject);
    }
}

public class EmailLinkBuilderTests
{
    [Fact]
    public void BuildResetUrl_NormalizesTrailingSlash()
    {
        var url = EmailLinkBuilder.BuildResetUrl("https://backoffice.test/", "tok/en+code");

        Assert.Equal("https://backoffice.test/reset-password?token=tok%2Fen%2Bcode", url);
    }

    [Fact]
    public void BuildResetUrl_EmptyBaseUrl_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => EmailLinkBuilder.BuildResetUrl("", "tok"));
    }
}
