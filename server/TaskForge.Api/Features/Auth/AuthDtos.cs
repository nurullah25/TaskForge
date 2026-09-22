using System.ComponentModel.DataAnnotations;

namespace TaskForge.Api.Features.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MaxLength(100)]
    public string FullName { get; set; } = "";

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public record UserDto(int Id, string Email, string FullName);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

// What the service hands back to the controller. The refresh token goes into a cookie,
// never into the response body.
public record AuthResult(AuthResponse Response, IssuedToken RefreshToken);
