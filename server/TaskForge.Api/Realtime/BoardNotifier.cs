using Microsoft.AspNetCore.SignalR;
using TaskForge.Api.Features.Tasks;

namespace TaskForge.Api.Realtime;

// Sends board changes to everyone else looking at the same board. Called from the services
// after SaveChanges, so clients never hear about a change that wasn't stored.
public class BoardNotifier(IHubContext<AppHub> hub, IHttpContextAccessor httpContextAccessor)
{
    public Task TaskCreated(int boardId, TaskCardDto card) => Send(boardId, "TaskCreated", card);

    public Task TaskUpdated(int boardId, TaskCardDto card) => Send(boardId, "TaskUpdated", card);

    public Task TaskMoved(int boardId, TaskCardDto card) => Send(boardId, "TaskMoved", card);

    public Task TaskDeleted(int boardId, int taskId) => Send(boardId, "TaskDeleted", taskId);

    // Columns change rarely, so the clients just reload the board.
    public Task ColumnsChanged(int boardId) => Send(boardId, "ColumnsChanged", boardId);

    private Task Send(int boardId, string eventName, object payload)
    {
        // The browser that made the change already updated itself, so it is left out.
        // Its connection id travels with the request in a header.
        var connectionId = httpContextAccessor.HttpContext?.Request.Headers["X-Connection-Id"].FirstOrDefault();

        var clients = string.IsNullOrEmpty(connectionId)
            ? hub.Clients.Group(AppHub.BoardGroup(boardId))
            : hub.Clients.GroupExcept(AppHub.BoardGroup(boardId), connectionId);

        return clients.SendAsync(eventName, payload);
    }
}
