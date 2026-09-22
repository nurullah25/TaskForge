using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskForge.Api.Common;

namespace TaskForge.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    private const string RefreshCookie = "tf_refresh";

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        SetRefreshCookie(result.RefreshToken);
        return CreatedAtAction(nameof(Me), result.Response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        SetRefreshCookie(result.RefreshToken);
        return result.Response;
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        var token = Request.Cookies[RefreshCookie];
        if (string.IsNullOrEmpty(token))
            throw new UnauthorizedException("You are not signed in.");

        var result = await authService.RefreshAsync(token);
        SetRefreshCookie(result.RefreshToken);
        return result.Response;
    }

    // Anonymous so a user with an expired access token can still sign out.
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(token))
            await authService.LogoutAsync(token);

        Response.Cookies.Delete(RefreshCookie, CreateCookieOptions());
        return NoContent();
    }

    [HttpGet("me")]
    public Task<UserDto> Me() => authService.GetCurrentUserAsync();

    private void SetRefreshCookie(IssuedToken token)
    {
        var options = CreateCookieOptions();
        options.Expires = token.ExpiresAt;
        Response.Cookies.Append(RefreshCookie, token.Value, options);
    }

    // HttpOnly keeps the token away from JavaScript (and XSS). SameSite=Strict stops other
    // sites from triggering a refresh, and the path limits the cookie to the auth endpoints.
    private static CookieOptions CreateCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth"
    };
}
