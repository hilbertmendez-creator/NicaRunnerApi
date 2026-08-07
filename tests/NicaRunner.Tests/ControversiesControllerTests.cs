using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using NicaRunner.Api.Controllers;

namespace NicaRunner.Tests;

public class ControversiesControllerTests
{
    private const string AdminRole = "Administrador";

    [Fact]
    public void Controller_RequiereAutenticacion_EnTodaLaClase()
    {
        var authorize = typeof(ControversiesController).GetCustomAttributes<AuthorizeAttribute>(inherit: true);

        Assert.Contains(authorize, a => string.IsNullOrWhiteSpace(a.Roles));
    }

    [Theory]
    [InlineData("Resolve")]
    [InlineData("GetAll")]
    [InlineData("GetSummary")]
    public void Acciones_RequierenUsuarioAutenticado(string action)
    {
        var method = typeof(ControversiesController).GetMethod(action);

        Assert.NotNull(method);
        // Sin [AllowAnonymous], el [Authorize] de clase cubre la autenticación.
        Assert.Empty(method!.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Resolve_SoloAdministrador_PorAtributoEnAccion()
    {
        var method = typeof(ControversiesController).GetMethod("Resolve")!;
        var authorize = method.GetCustomAttributes<AuthorizeAttribute>();

        Assert.Contains(authorize, a => a.Roles?.Split(',')?.Contains(AdminRole) == true);
    }

    [Fact]
    public void Controller_ExponeListadoYResolucion()
    {
        var methods = typeof(ControversiesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name);

        Assert.Contains("GetAll", methods);
        Assert.Contains("GetSummary", methods);
        Assert.Contains("Resolve", methods);
    }
}