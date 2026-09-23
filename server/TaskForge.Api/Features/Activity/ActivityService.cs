using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Activity;

public record ActivityDto(
    long Id,
    ActivityType Type,
    MemberSummaryDto User,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt,
    int? TaskId,
    int? TaskNumber,
    string? TaskTitle);

public class ActivityService(AppDbContext db, AccessService access)
{
    public async Task<List<ActivityDto>> GetForTaskAsync(int taskId)
    {
        var projectId = await db.Tasks
            .Where(t => t.Id == taskId)
            .Select(t => (int?)t.ProjectId)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Task not found.");

        await access.RequireProjectRoleAsync(projectId);

        return await db.ActivityLogs
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(100)
            .Select(Projection)
            .ToListAsync();
    }

    public async Task<PagedResult<ActivityDto>> GetForProjectAsync(int projectId, PageQuery page)
    {
        await access.RequireProjectRoleAsync(projectId);

        var query = db.ActivityLogs.Where(a => a.ProjectId == projectId);
        var total = await query.CountAsync();

        var entries = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(Projection)
            .ToListAsync();

        return new PagedResult<ActivityDto>(entries, page.Page, page.PageSize, total);
    }

    private static System.Linq.Expressions.Expression<Func<ActivityLog, ActivityDto>> Projection =>
        a => new ActivityDto(
            a.Id,
            a.Type,
            new MemberSummaryDto(a.User.Id, a.User.FullName, a.User.Email),
            a.OldValue,
            a.NewValue,
            a.CreatedAt,
            a.TaskId,
            a.Task == null ? null : a.Task.Number,
            a.Task == null ? null : a.Task.Title);
}
