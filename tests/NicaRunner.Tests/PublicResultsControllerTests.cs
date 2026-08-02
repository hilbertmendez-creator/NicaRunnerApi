using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NicaRunner.Api.Controllers;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.PublicResults;
using NicaRunner.Application.PublicResults.Dtos;

namespace NicaRunner.Tests;

// design.md Decisión 7 / spec.md "Opaque Share Key Addressing": el controller delega
// entero en el service (mismo patrón que GetRunnerResult) — ni valida de más ni atrapa
// excepciones, ExceptionHandlingMiddleware ya traduce NotFoundException a 404.
//
// El patrón de ruta [A-Za-z0-9_-]{16,32} vive como restricción de ASP.NET en el atributo
// [HttpGet] del controller (regex con llaves escapadas {{16,32}} para el route template)
// y NO es extraíble en runtime desde el atributo sin reflexión pesada — el proyecto no
// tiene precedente de WebApplicationFactory/TestServer. Este mismo patrón, sin escapar,
// se prueba acá de forma independiente para fijar el contrato de longitud/alfabeto; debe
// mantenerse sincronizado a mano con el literal del atributo (ver PublicResultsController).
public class PublicResultsControllerTests
{
    private static readonly Regex ShareKeyRoutePattern = new("^[A-Za-z0-9_-]{16,32}$");

    [Theory]
    [InlineData("AAAAAAAAAAAAAAAA")] // 16 chars, el mínimo
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")] // 32 chars, el máximo
    [InlineData("dGhpc0lzQVZhbGlkS2V5MDE")] // 22 chars, forma típica de ShareKeyGenerator
    [InlineData("abc-DEF_123-_abcDEF12a")]
    public void ShareKeyRoutePattern_ClaveValida_Coincide(string shareKey) =>
        Assert.Matches(ShareKeyRoutePattern, shareKey);

    [Theory]
    [InlineData("")]
    [InlineData("cortaCorta1234")] // 14 chars, por debajo del mínimo
    [InlineData("AAAAAAAAAAAAAAA")] // 15 chars, uno por debajo del mínimo
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")] // 33 chars, uno por encima del máximo
    [InlineData("clave con espacios1234567")]
    [InlineData("clave/con/barras1234567890")]
    [InlineData("clave.con.puntos.12345678")]
    [InlineData("clave+con+mas1234567890ab")]
    [InlineData("clave@arroba1234567890abc")]
    public void ShareKeyRoutePattern_ClaveInvalida_NoCoincide(string shareKey) =>
        Assert.DoesNotMatch(ShareKeyRoutePattern, shareKey);

    [Fact]
    public async Task GetRunnerByShareKey_ClaveValida_Devuelve200ConElDto()
    {
        var dto = new PublicRunnerShareDto(
            "Carrera Test", "Un eslogan", new DateTime(2026, 6, 1),
            "Ana", "Pérez", null, "101",
            "Juvenil", 5m,
            null, null, null, null, null, null);

        var service = new Mock<IPublicResultService>();
        service.Setup(s => s.GetRunnerByShareKeyAsync("claveValidaXXXXXXXXXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new PublicResultsController(service.Object);

        var response = await controller.GetRunnerByShareKey("claveValidaXXXXXXXXXX", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(dto, okResult.Value);
    }

    [Fact]
    public async Task GetRunnerByShareKey_ClaveDesconocida_PropagaNotFoundException()
    {
        var service = new Mock<IPublicResultService>();
        service.Setup(s => s.GetRunnerByShareKeyAsync("no-existe-XXXXXXXXXXX", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("El enlace no es válido."));

        var controller = new PublicResultsController(service.Object);

        // El controller no atrapa la excepción — ExceptionHandlingMiddleware la traduce a
        // 404 en el pipeline real. Acá se prueba que el controller no la absorbe ni la
        // reemplaza por otra cosa (mismo contrato que GetRunnerResult).
        await Assert.ThrowsAsync<NotFoundException>(
            () => controller.GetRunnerByShareKey("no-existe-XXXXXXXXXXX", CancellationToken.None));
    }
}
