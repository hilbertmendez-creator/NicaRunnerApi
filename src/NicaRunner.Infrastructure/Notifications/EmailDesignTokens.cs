namespace NicaRunner.Infrastructure.Notifications;

/// <summary>
/// Tokens del tema <c>brand</c> del backoffice — ver frontend/src/index.css.
/// </summary>
public static class EmailDesignTokens
{
    public const string BgApp = "#F5F9FF";
    public const string BgCard = "#FFFFFF";
    public const string HeaderFrom = "#0D47A1";
    public const string HeaderTo = "#08306B";
    public const string BorderCard = "#D6EAFF";
    public const string TextHi = "#11243F";
    public const string TextLo = "#5B6B82";
    public const string TextXs = "#9AA5B1";
    public const string Accent = "#1565FF";
    public const string AccentBg = "#EEF3FF";
    public const string AccentBorder = "#BFDBFE";
    public const string AccentText = "#1D4ED8";
    public const string CtaFrom = "#42A5F5";
    public const string CtaTo = "#1565FF";
    public const string FontSans = "'Inter', system-ui, 'Segoe UI', Arial, sans-serif";
    public const string FontMono = "'JetBrains Mono', ui-monospace, 'Courier New', monospace";
    public const int RadiusCard = 14;
    public const int RadiusBtn = 10;

    // Isotipo simplificado email-safe (sin filtros SVG complejos)
    public const string LogoSvgInline =
        """<svg xmlns="http://www.w3.org/2000/svg" width="32" height="31" viewBox="0 0 48 46" role="img" aria-label="NicaRunner"><path fill="#863bff" d="M25.946 44.938c-.664.845-2.021.375-2.021-.698V33.937a2.26 2.26 0 0 0-2.262-2.262H10.287c-.92 0-1.456-1.04-.92-1.788l7.48-10.471c1.07-1.497 0-3.578-1.842-3.578H1.237c-.92 0-1.456-1.04-.92-1.788L10.013.474c.214-.297.556-.474.92-.474h28.894c.92 0 1.456 1.04.92 1.788l-7.48 10.471c-1.07 1.498 0 3.579 1.842 3.579h11.377c.943 0 1.473 1.088.89 1.83L25.947 44.94z"/></svg>""";
}
