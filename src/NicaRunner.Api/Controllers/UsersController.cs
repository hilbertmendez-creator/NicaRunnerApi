using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NicaRunner.Application.Users;
using NicaRunner.Application.Users.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public class UsersController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken ct) =>
        Ok(await userManagementService.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await userManagementService.CreateAsync(request, ct);
        return Ok(user);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await userManagementService.UpdateAsync(GetUserId(), id, request, ct));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
