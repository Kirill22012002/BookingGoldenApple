using BGA.API.Dtos;
using BGA.API.Extensions;
using BGA.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BGA.API.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(IUserService userService) : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
    {
        await userService.RegisterAsync(dto.Login, dto.Password, dto.Role.ToUserRole(), cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto, CancellationToken cancellationToken)
    {
        var token = await userService.LoginAsync(dto.Login, dto.Password, cancellationToken);
        return Ok(new LoginResponseDto { Token = token });
    }
}
