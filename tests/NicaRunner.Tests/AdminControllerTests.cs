using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NicaRunner.Api.Controllers;
using NicaRunner.Application.Admin;
using NicaRunner.Application.Notifications;
using NicaRunner.Application.Races;
using NicaRunner.Application.Races.Dtos;

namespace NicaRunner.Tests;

// Cubre solo el endpoint nuevo (stale-sweep): el guard X-Admin-Secret es idéntico al de
// los otros endpoints de AdminController, no re-testeado acá.
public class AdminControllerTests
{
    private readonly Mock<IRefreshTokenCleanupService> _refreshTokenCleanup = new();
    private readonly Mock<IPublicTokenCleanupService> _publicTokenCleanup = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IStaleRaceSweepService> _staleRaceSweep = new();

    private const string ConfiguredSecret = "test-secret";

    private AdminController BuildController(string? headerSecret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:CleanupSecret"] = ConfiguredSecret })
            .Build();

        var controller = new AdminController(
            _refreshTokenCleanup.Object,
            _publicTokenCleanup.Object,
            _notificationService.Object,
            _staleRaceSweep.Object,
            configuration,
            NullLogger<AdminController>.Instance);

        var httpContext = new DefaultHttpContext();
        if (headerSecret is not null)
            httpContext.Request.Headers["X-Admin-Secret"] = headerSecret;

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task StaleRaceSweep_SecretCorrecto_DevuelveLaListaDelService()
    {
        var expected = new StaleRaceSweepResult([new StaleRaceDto(1, "5K", DateTime.UtcNow.AddHours(-13))]);
        _staleRaceSweep.Setup(s => s.RunAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var response = await BuildController(ConfiguredSecret).StaleRaceSweep(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task StaleRaceSweep_SecretIncorrecto_Devuelve401()
    {
        var response = await BuildController("wrong-secret").StaleRaceSweep(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response.Result);
        _staleRaceSweep.Verify(s => s.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StaleRaceSweep_SinHeader_Devuelve401()
    {
        var response = await BuildController(headerSecret: null).StaleRaceSweep(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response.Result);
        _staleRaceSweep.Verify(s => s.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
