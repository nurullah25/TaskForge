using System.ComponentModel.DataAnnotations;

namespace TaskForge.Api.Features.Auth;

public class JwtSettings
{
    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    // HMAC-SHA256 needs at least a 256-bit key. Set it with user secrets or the
    // Jwt__Key environment variable outside of development.
    [Required, MinLength(32)]
    public string Key { get; set; } = "";

    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
