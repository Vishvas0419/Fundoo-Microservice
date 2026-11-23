using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fundoo.User.Api.Contracts;
using Fundoo.User.Application.Interfaces;
using System.Security.Claims;

namespace Fundoo.User.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class ExternalAuthController : ControllerBase
{
    private const string ExternalCookieScheme = "ExternalCookie";
    private const string GoogleOidcScheme = "GoogleOIDC";
    private readonly IAuthService _authService;

    public ExternalAuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleComplete), "ExternalAuth")
        };

        return Challenge(properties, GoogleOidcScheme);
    }

    [AllowAnonymous]
    [HttpGet("google-complete")]
    public async Task<IActionResult> GoogleComplete(
        CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(ExternalCookieScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            await HttpContext.SignOutAsync(ExternalCookieScheme);
            return Unauthorized(new
            {
                message = "Google sign-in failed."
            });
        }

        var principal = result.Principal;

        var googleSubject =
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        var email =
            principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");

        var emailVerified = principal.FindFirstValue("email_verified");
        var name = principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(googleSubject) ||
            string.IsNullOrWhiteSpace(email) ||
            !string.Equals(
                emailVerified,
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(ExternalCookieScheme);

            return Unauthorized(new
            {
                message = "Google did not provide a verified email and subject."
            });
        }

        try
        {
            var accessToken = await _authService.SignInWithExternalProviderAsync(
                provider: "Google",
                providerSubject: googleSubject,
                email: email,
                displayName: name,
                cancellationToken: cancellationToken);

            return Ok(new AuthResponse
            {
                Message = "Google sign-in successful.",
                AccessToken = accessToken
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        finally
        {
            await HttpContext.SignOutAsync(ExternalCookieScheme);
        }
    }
}
