using System.Net;

namespace NicaRunner.Infrastructure.Notifications;

/// <summary>
/// Layout HTML base para correos — tema brand del backoffice.
/// </summary>
internal static class EmailLayoutBuilder
{
    public static string Wrap(string bodyHtml, string productName, string tagline)
    {
        var year = DateTime.UtcNow.Year;
        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <meta http-equiv="X-UA-Compatible" content="IE=edge">
              <title>{productName}</title>
            </head>
            <body style="margin:0;padding:0;background-color:{EmailDesignTokens.BgApp};font-family:{EmailDesignTokens.FontSans};">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:{EmailDesignTokens.BgApp};">
                <tr>
                  <td align="center" style="padding:24px 16px;">
                    <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="max-width:600px;width:100%;">
                      <tr>
                        <td style="background:linear-gradient(160deg, {EmailDesignTokens.HeaderFrom} 0%, {EmailDesignTokens.HeaderTo} 100%);border-radius:{EmailDesignTokens.RadiusCard}px {EmailDesignTokens.RadiusCard}px 0 0;padding:24px 32px;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                              <td width="40" valign="middle" style="padding-right:12px;">
                                {EmailDesignTokens.LogoSvgInline}
                              </td>
                              <td valign="middle">
                                <div style="font-size:18px;font-weight:700;color:#FFFFFF;line-height:1.2;">{WebUtility.HtmlEncode(productName)}</div>
                                <div style="font-size:11px;font-weight:400;color:rgba(255,255,255,0.65);margin-top:4px;">{tagline}</div>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="background-color:{EmailDesignTokens.BgCard};border:1px solid {EmailDesignTokens.BorderCard};border-top:none;border-radius:0 0 {EmailDesignTokens.RadiusCard}px {EmailDesignTokens.RadiusCard}px;padding:32px;">
                          {bodyHtml}
                        </td>
                      </tr>
                      <tr>
                        <td align="center" style="padding:20px 16px 0;">
                          <p style="margin:0;font-size:11px;color:{EmailDesignTokens.TextXs};line-height:1.5;">
                            © {year} NicaRunner — Gestión de competencias de atletismo
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
