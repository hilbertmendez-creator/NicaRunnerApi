namespace NicaRunner.Api.Auth;

/// <summary>
/// Nombres de las cookies httpOnly que llevan el JWT y el refresh token para
/// clientes browser. Compartido entre AuthController (que las setea/lee) y
/// Program.cs (que las lee como fallback de autenticación y aplica CSRF).
/// El body de las respuestas de auth sigue devolviendo los mismos valores en
/// JSON — la app Android no usa cookies y sigue leyendo de ahí sin cambios.
/// </summary>
public static class AuthCookieNames
{
    public const string AccessToken = "nr_at";
    public const string RefreshToken = "nr_rt";
    public const string Csrf = "nr_csrf";
    public const string CsrfHeader = "X-CSRF-Token";
}
