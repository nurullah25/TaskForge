using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskForge.Api.Common;

// Gives services the signed-in user's id without passing it through every method call.
public class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    public int Id
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(subject, out var id)
                ? id
                : throw new UnauthorizedException("You are not signed in.");
        }
    }
}
