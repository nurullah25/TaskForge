using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;
using TaskForge.Api.Realtime;

namespace TaskForge.Api.Features.Notifications;

public record NotificationDto(
    long Id,
    NotificationType Type,
    string Message,
    int? TaskId,
    int? BoardId,
    bool IsRead,
    DateTime CreatedAt);

public class NotificationService(AppDbContext db, CurrentUser currentUser, IHubContext<AppHub> hub)
{
    public async Task<PagedResult<NotificationDto>> GetMineAsync(PageQuery page, bool unreadOnly)
    {
        var userId = currentUser.Id;
        var query = db.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync();
        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(Projection)
            .ToListAsync();

        return new PagedResult<NotificationDto>(notifications, page.Page, page.PageSize, total);
    }

    public Task<int> GetUnreadCountAsync()
    {
        var userId = currentUser.Id;
        return db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkReadAsync(long notificationId)
    {
        var userId = currentUser.Id;
        var updated = await db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        if (updated == 0)
            throw new NotFoundException("Notification not found.");
    }

    public async Task MarkAllReadAsync()
    {
        var userId = currentUser.Id;
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    // Called after the change that caused it has been saved. Nobody is notified about
    // their own actions, so this quietly does nothing for the current user.
    public async Task SendAsync(int userId, NotificationType type, string message, int? taskId = null)
    {
        if (userId == currentUser.Id)
            return;

        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            TaskId = taskId
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var dto = await db.Notifications.Where(n => n.Id == notification.Id).Select(Projection).SingleAsync();
        await hub.Clients.User(userId.ToString()).SendAsync("NotificationReceived", dto);
    }

    private static System.Linq.Expressions.Expression<Func<Notification, NotificationDto>> Projection =>
        n => new NotificationDto(
            n.Id,
            n.Type,
            n.Message,
            n.TaskId,
            n.Task == null ? null : n.Task.Column.BoardId,
            n.IsRead,
            n.CreatedAt);
}
