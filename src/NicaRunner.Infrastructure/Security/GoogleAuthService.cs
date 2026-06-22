using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using NicaRunner.Application.Common.Interfaces;

namespace NicaRunner.Infrastructure.Security;

public class GoogleAuthService(IOptions<GoogleAuthSettings> options) : IGoogleAuthService
{
    private readonly GoogleAuthSettings _settings = options.Value;

    public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_settings.ClientId]
            });
        }
        catch (InvalidJwtException)
        {
            return null;
        }

        if (!payload.EmailVerified) return null;

        return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name);
    }
}
