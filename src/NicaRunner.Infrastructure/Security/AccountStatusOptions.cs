namespace NicaRunner.Infrastructure.Security;

// backoffice-user-status-toggle PR3 (design.md D3) -- interruptor de configuración para
// el chequeo de IsActive por request. EnforcePerRequest=false lo desactiva sin deploy
// (mismo patrón que LockoutOptions.Threshold=0): ni cache ni BD se consultan.
public class AccountStatusOptions
{
    public bool EnforcePerRequest { get; set; } = true;
    public int CacheSeconds { get; set; } = 30;

    // Lo mismo para el cambio obligatorio de contraseña (PasswordChangeRequiredMiddleware):
    // en false, el panel vuelve a ser el único que lo exige.
    public bool EnforcePasswordChange { get; set; } = true;
}
