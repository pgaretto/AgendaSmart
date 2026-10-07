using Microsoft.AspNetCore.Mvc;
using SmartAgenda.Api.Models;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request.Email, request.Password);
        if (user is null)
        {
            return Conflict(new { message = "El email ya está registrado." });
        }

        var response = new UserResponse { Id = user.Id, Email = user.Email };
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
