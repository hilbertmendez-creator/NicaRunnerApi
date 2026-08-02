using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NicaRunner.Api.Controllers;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Controversias;
using NicaRunner.Application.Controversias.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

// Sin WebApplicationFactory: authz HTTP 403 lo cubre [Authorize] + ForbiddenException del service.
// Aquí fijamos delegación del controller y la matriz de roles en atributos.
public class ControversiasControllerTests
{
    private readonly Mock<IDisputeService> _service = new();

    private ControversiasController BuildController(UserRole role = UserRole.Administrador, int userId = 9)
    {
        var controller = new ControversiasController(_service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Role, role.ToString())
                    ], "Test"))
                }
            }
        };
        return controller;
    }

    private static TimingDisputeDto SampleDto(DisputeEstado estado = DisputeEstado.Disputa) => new(
        Id: 7, RaceId: 1, ResultId: 40, RunnerId: 5, Dorsal: "118", CorredorNombre: "Ana Pérez",
        ChipSeconds: 100, CheckpointSeconds: 101, CameraSeconds: null, Estado: estado,
        CapturistaNombre: "M. López", CapturaNote: null, ResolvedByUserId: null,
        ResolvedAt: null, UpdatedAt: DateTime.UtcNow);

    [Fact]
    public async Task GetAll_SinFilas_DevuelveListaVacia()
    {
        _service.Setup(s => s.ListAsync(1, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var response = await BuildController(UserRole.Lector).GetAll(1, null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var list = Assert.IsType<List<TimingDisputeDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetOpenCount_SinAbiertas_DevuelveCero()
    {
        _service.Setup(s => s.CountOpenAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var response = await BuildController(UserRole.Lector).GetOpenCount(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var dto = Assert.IsType<OpenDisputeCountDto>(ok.Value);
        Assert.Equal(0, dto.Count);
    }

    [Fact]
    public async Task Resolve_Admin_DelegaYDevuelveDto()
    {
        var request = new ResolveDisputeRequest(DisputeEstado.Disputa);
        var dto = SampleDto();
        _service.Setup(s => s.ResolveAsync(
                1, 7, request, 9, UserRole.Administrador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var response = await BuildController().Resolve(1, 7, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task Resolve_Lector_PropagaForbiddenException()
    {
        var request = new ResolveDisputeRequest(DisputeEstado.Disputa);
        _service.Setup(s => s.ResolveAsync(
                1, 7, request, 2, UserRole.Lector, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Solo un administrador puede resolver controversias."));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            BuildController(UserRole.Lector, userId: 2).Resolve(1, 7, request, CancellationToken.None));
    }

    [Fact]
    public void AuthorizeAttributes_GetAdminLector_PatchSoloAdmin()
    {
        var type = typeof(ControversiasController);
        var classAuth = type.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuth);
        Assert.Equal($"{nameof(UserRole.Administrador)},{nameof(UserRole.Lector)}", classAuth!.Roles);

        var patch = type.GetMethod(nameof(ControversiasController.Resolve))!;
        var patchAuth = patch.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(patchAuth);
        Assert.Equal(nameof(UserRole.Administrador), patchAuth!.Roles);
    }
}
