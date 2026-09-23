using Microsoft.AspNetCore.Mvc;
using TaskForge.Api.Common;

namespace TaskForge.Api.Features.Notifications;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<NotificationDto>> GetMine([FromQuery] PageQuery query, bool unreadOnly = false) =>
        notifications.GetMineAsync(query, unreadOnly);

    [HttpGet("unread-count")]
    public Task<int> GetUnreadCount() => notifications.GetUnreadCountAsync();

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        await notifications.MarkReadAsync(id);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await notifications.MarkAllReadAsync();
        return NoContent();
    }
}
