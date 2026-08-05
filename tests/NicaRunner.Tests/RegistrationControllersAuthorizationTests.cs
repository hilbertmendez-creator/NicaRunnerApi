using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using NicaRunner.Api.Controllers;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

// tasks.md 2.10 "unauthenticated admin-route rejection": sin WebApplicationFactory (el
// proyecto no tiene ese precedente — ver el comentario en PublicResultsControllerTests).
// Mismo patrón que ControversiasControllerTests.AuthorizeAttributes_GetAdminLector_PatchSoloAdmin:
// se fija la matriz de [Authorize]/[AllowAnonymous] por reflexión; el 401/403 real en
// runtime lo produce el middleware de autenticación/autorización de ASP.NET Core, que ya
// está probado por el framework, no por este proyecto.
public class RegistrationControllersAuthorizationTests
{
    [Fact]
    public void RegistrationsController_RequiereRolAdministrador()
    {
        var classAuth = typeof(RegistrationsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Equal(nameof(UserRole.Administrador), classAuth!.Roles);
    }

    [Fact]
    public void ReservedDorsalsController_RequiereRolAdministrador()
    {
        var classAuth = typeof(ReservedDorsalsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Equal(nameof(UserRole.Administrador), classAuth!.Roles);
    }

    // tasks.md 2.12/2.13: creación/revocación del RegistrationLink es admin-only, igual
    // que PublicTokensController para PublicResultToken.
    [Fact]
    public void RegistrationLinksController_RequiereRolAdministrador()
    {
        var classAuth = typeof(RegistrationLinksController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Equal(nameof(UserRole.Administrador), classAuth!.Roles);
    }

    // tasks.md 2.11/2.13: configurar Capacidad/Precio de una RaceCategory es admin-only;
    // el resto del controller solo exige [Authorize] (sin rol) para GET.
    [Fact]
    public void RaceCategoriesController_ConfigureRequiereRolAdministrador()
    {
        var method = typeof(RaceCategoriesController).GetMethod(nameof(RaceCategoriesController.Configure));
        Assert.NotNull(method);

        var methodAuth = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(methodAuth);
        Assert.Equal(nameof(UserRole.Administrador), methodAuth!.Roles);
    }

    [Fact]
    public void PublicRegistrationController_EsAnonimoYTieneRateLimitPropio()
    {
        var type = typeof(PublicRegistrationController);

        Assert.NotNull(type.GetCustomAttribute<AllowAnonymousAttribute>());

        var rateLimit = type.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimit);
        Assert.Equal("public-registration", rateLimit!.PolicyName);
    }
}
