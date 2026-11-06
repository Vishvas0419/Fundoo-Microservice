using Fundoo.User.Api.Contracts;
using Fundoo.User.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fundoo.User.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _authService.RegisterAsync(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                cancellationToken);

            return Ok(new AuthResponse
            {
                Message = message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var token = await _authService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (token == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        return Ok(new AuthResponse
        {
            Message = "Login successful.",
            AccessToken = token
        });
    }


    [EnableRateLimiting("fixed")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
    [FromBody] ForgotPasswordRequest request,
    CancellationToken cancellationToken)
    {
        var token = await _authService.ForgotPasswordAsync(
            request.Email,
            cancellationToken);

        var response = new AuthResponse
        {
            Message = "If an account exists for this email, password reset instructions will be provided."
        };

        if (_environment.IsDevelopment())
        {
            response.ResetToken = token;
        }

        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _authService.ResetPasswordAsync(
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!success)
        {
            return BadRequest(new
            {
                message = "The reset token is invalid, expired, or already used."
            });
        }

        return Ok(new
        {
            message = "Password reset successfully."
        });
    }

}
