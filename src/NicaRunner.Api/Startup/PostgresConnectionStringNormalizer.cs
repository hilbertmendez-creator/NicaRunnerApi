namespace NicaRunner.Api.Startup;

/// <summary>
/// Neon —donde vive la Postgres de producción— expone la connection string en
/// formato URI (postgres://usuario:password@host:puerto/db), pero Npgsql solo
/// entiende el formato keyword=value (Host=...;Username=...;...). Sin esto,
/// NpgsqlConnectionStringBuilder lanza ArgumentException apenas arranca el
/// contenedor ("Format of the initialization string does not conform to
/// specification starting at index 0") — verificado en el primer deploy real.
/// Si la cadena ya viene en formato keyword=value (como en dev contra un
/// Postgres local), se devuelve sin tocar.
///
/// Antes esta clase normalizaba la URI del Postgres administrado de Render;
/// tras migrar la base a Neon el formato de entrada es el mismo, así que la
/// lógica no cambió — solo cambió de quién viene la cadena.
/// </summary>
public static class PostgresConnectionStringNormalizer
{
    public static string Normalize(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port == -1 ? 5432 : uri.Port;

        // Require (no Prefer): esta rama solo la ejercita la URI de Neon en
        // producción (dev/test usan Sqlite), y Neon siempre soporta TLS con
        // certificado válido — Prefer permitía degradar silenciosamente a texto
        // plano si el handshake TLS fallaba. Sin Trust Server Certificate: Neon
        // usa un certificado firmado por una CA pública, así que la validación
        // default de Npgsql contra el store del sistema alcanza; confiar en
        // cualquier certificado exponía a un MITM.
        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require";
    }
}
