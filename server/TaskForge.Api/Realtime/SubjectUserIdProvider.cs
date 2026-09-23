using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskForge.Api.Realtime;

// SignalR looks for a NameIdentifier claim by default. Our tokens keep the standard "sub"
// claim instead, so Clients.User(id) is told where to find the user id.
public class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
