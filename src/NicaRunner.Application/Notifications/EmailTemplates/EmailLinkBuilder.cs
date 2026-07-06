namespace NicaRunner.Application.Notifications.EmailTemplates;

public static class EmailLinkBuilder
{
    public static string BuildResetUrl(string baseUrl, string token)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Frontend:BaseUrl no configurado.");

        var normalized = baseUrl.TrimEnd('/');
        var url = $"{normalized}/reset-password?token={Uri.EscapeDataString(token)}";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("URL de reset inválida.");

        return url;
    }
}
