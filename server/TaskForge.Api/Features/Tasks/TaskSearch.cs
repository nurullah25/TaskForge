using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;
using TaskForge.Api.Features.Labels;

namespace TaskForge.Api.Features.Tasks;

public enum TaskSort
{
    Newest = 0,
    DueDate = 1,
    Priority = 2
}

public class TaskSearchQuery : PageQuery
{
    public string? Text { get; set; }
    public int? AssigneeId { get; set; }
    public bool Unassigned { get; set; }
    public TaskPriority? Priority { get; set; }
    public int? ColumnId { get; set; }
    public ColumnCategory? Category { get; set; }
    public int? LabelId { get; set; }
    public DateOnly? DueFrom { get; set; }
    public DateOnly? DueTo { get; set; }
    public bool Overdue { get; set; }
    public TaskSort Sort { get; set; } = TaskSort.Newest;
}

public record TaskListItemDto(
    int Id,
    int Number,
    string ProjectKey,
    string Title,
    TaskPriority Priority,
    DateOnly? DueDate,
    MemberSummaryDto? Assignee,
    List<LabelDto> Labels,
    int BoardId,
    string ColumnName,
    ColumnCategory Category,
    DateTime? CompletedAt);

// Filtering lives in its own class because the query grows one condition at a time and
// would otherwise crowd TaskService.
public class TaskSearchService(AppDbContext db, AccessService access)
{
    public async Task<PagedResult<TaskListItemDto>> SearchAsync(int projectId, TaskSearchQuery query)
    {
        await access.RequireProjectRoleAsync(projectId);

        // Nothing runs until ToListAsync: each condition just adds to the SQL.
        var tasks = db.Tasks.Where(t => t.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var text = query.Text.Trim();
            tasks = tasks.Where(t => t.Title.Contains(text) || (t.Description != null && t.Description.Contains(text)));
        }

        if (query.Unassigned)
            tasks = tasks.Where(t => t.AssigneeId == null);
        else if (query.AssigneeId is { } assigneeId)
            tasks = tasks.Where(t => t.AssigneeId == assigneeId);

        if (query.Priority is { } priority)
            tasks = tasks.Where(t => t.Priority == priority);

        if (query.ColumnId is { } columnId)
            tasks = tasks.Where(t => t.ColumnId == columnId);

        if (query.Category is { } category)
            tasks = tasks.Where(t => t.Column.Category == category);

        if (query.LabelId is { } labelId)
            tasks = tasks.Where(t => t.Labels.Any(l => l.LabelId == labelId));

        if (query.DueFrom is { } dueFrom)
            tasks = tasks.Where(t => t.DueDate >= dueFrom);

        if (query.DueTo is { } dueTo)
            tasks = tasks.Where(t => t.DueDate <= dueTo);

        if (query.Overdue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            tasks = tasks.Where(t => t.DueDate < today && t.CompletedAt == null);
        }

        var total = await tasks.CountAsync();

        tasks = query.Sort switch
        {
            // Tasks without a due date go last instead of first.
            TaskSort.DueDate => tasks.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate).ThenBy(t => t.Id),
            TaskSort.Priority => tasks.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate == null).ThenBy(t => t.DueDate),
            _ => tasks.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
        };

        var items = await tasks
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(ListItem)
            .ToListAsync();

        return new PagedResult<TaskListItemDto>(items, query.Page, query.PageSize, total);
    }

    public static System.Linq.Expressions.Expression<Func<TaskItem, TaskListItemDto>> ListItem { get; } =
        t => new TaskListItemDto(
            t.Id,
            t.Number,
            t.Project.Key,
            t.Title,
            t.Priority,
            t.DueDate,
            t.Assignee == null ? null : new MemberSummaryDto(t.Assignee.Id, t.Assignee.FullName, t.Assignee.Email),
            t.Labels.OrderBy(tl => tl.Label.Name).Select(tl => new LabelDto(tl.Label.Id, tl.Label.Name, tl.Label.Color)).ToList(),
            t.Column.BoardId,
            t.Column.Name,
            t.Column.Category,
            t.CompletedAt);
}
