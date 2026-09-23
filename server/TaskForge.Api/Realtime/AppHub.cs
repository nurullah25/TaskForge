using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Realtime;

// One hub for the whole app. Clients join the group of the board they are looking at and
// leave it when they navigate away; notifications are sent to the user instead of a group.
[Authorize]
public class AppHub(AppDbContext db) : Hub
{
    public static string BoardGroup(int boardId) => $"board-{boardId}";

    public async Task JoinBoard(int boardId)
    {
        // Membership is checked here as well: joining a group is reading other people's work.
        var userId = int.Parse(Context.UserIdentifier ?? "0");
        var access = await db.Boards
            .Where(b => b.Id == boardId)
            .Select(b => new
            {
                OrganizationRole = b.Project.Organization.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (OrganizationRole?)m.Role)
                    .SingleOrDefault(),
                ProjectRole = b.Project.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (ProjectRole?)m.Role)
                    .SingleOrDefault()
            })
            .SingleOrDefaultAsync();

        if (AccessService.GetEffectiveProjectRole(access?.OrganizationRole, access?.ProjectRole) == null)
            throw new HubException("You don't have access to this board.");

        await Groups.AddToGroupAsync(Context.ConnectionId, BoardGroup(boardId));
    }

    public Task LeaveBoard(int boardId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, BoardGroup(boardId));
}
