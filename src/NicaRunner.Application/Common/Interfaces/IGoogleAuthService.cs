namespace NicaRunner.Application.Common.Interfaces;

public record GoogleUserInfo(string Sub, string Email, string Nombre);

/// <summary>Valida un ID Token de Google Sign-In y extrae la identidad del usuario.</summary>
public interface IGoogleAuthService
{
    /// <summary>Devuelve la identidad si el token es válido y el email está verificado; null en caso contrario.</summary>
    Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}
