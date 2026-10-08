using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAgenda.Api.Models;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        IAuthService authService,
        IJwtTokenGenerator jwtTokenGenerator,
        ICurrentUserService currentUserService)
    {
        _authService = authService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _currentUserService = currentUserService;
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

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _authService.ValidateCredentialsAsync(request.Email, request.Password);
        if (user is null)
        {
            return Unauthorized(new { message = "Email o contraseña incorrectos." });
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        return Ok(new LoginResponse { Token = token });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await _authService.GetByIdAsync(_currentUserService.UserId);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserResponse { Id = user.Id, Email = user.Email });
    }
}
