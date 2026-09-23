using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Tasks;

public class TaskService(AppDbContext db, CurrentUser currentUser, AccessService access)
{
    public async Task<TaskDetailsDto> GetAsync(int taskId)
    {
        var task = await LoadAsync(taskId);
        var role = await access.RequireProjectRoleAsync(task.ProjectId);

        return await ToDetailsAsync(task, role);
    }

    public async Task<TaskDetailsDto> CreateAsync(CreateTaskRequest request)
    {
        var column = await db.BoardColumns
            .Where(c => c.Id == request.ColumnId)
            .Select(c => new { c.Id, c.Board.ProjectId, c.Category })
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Column not found.");

        var role = await access.RequireProjectRoleAsync(column.ProjectId, ProjectRole.Contributor);
        await EnsureAssigneeIsMemberAsync(column.ProjectId, request.AssigneeId);

        var project = await db.Projects.SingleAsync(p => p.Id == column.ProjectId);
        var lastPosition = await db.Tasks
            .Where(t => t.ColumnId == column.Id)
            .MaxAsync(t => (double?)t.Position);

        var task = new TaskItem
        {
            ProjectId = project.Id,
            ColumnId = column.Id,
            Number = ++project.TaskCounter,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            ReporterId = currentUser.Id,
            DueDate = request.DueDate,
            Position = TaskOrdering.Between(lastPosition, null),
            CompletedAt = column.Category == ColumnCategory.Done ? DateTime.UtcNow : null
        };

        db.Tasks.Add(task);
        Log(task, ActivityType.TaskCreated, newValue: task.Title);

        if (task.AssigneeId != null)
            Log(task, ActivityType.TaskAssigned, newValue: await GetUserNameAsync(task.AssigneeId.Value));

        await SaveWithUniqueNumberRetryAsync(task, project);

        return await ToDetailsAsync(await LoadAsync(task.Id), role);
    }

    public async Task<TaskDetailsDto> UpdateAsync(int taskId, UpdateTaskRequest request)
    {
        var task = await LoadAsync(taskId);
        var role = await access.RequireProjectRoleAsync(task.ProjectId, ProjectRole.Contributor);
        await EnsureAssigneeIsMemberAsync(task.ProjectId, request.AssigneeId);

        // Detects the "two people edited the same task" case; SQL Server compares the
        // rowversion the client last saw with the one in the database.
        if (!string.IsNullOrEmpty(request.RowVersion))
            db.Entry(task).Property(t => t.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);

        var title = request.Title.Trim();
        if (title != task.Title)
            Log(task, ActivityType.TitleChanged, task.Title, title);

        if (request.Priority != task.Priority)
            Log(task, ActivityType.PriorityChanged, task.Priority.ToString(), request.Priority.ToString());

        if (request.AssigneeId != task.AssigneeId)
        {
            Log(task, ActivityType.TaskAssigned,
                task.AssigneeId == null ? null : await GetUserNameAsync(task.AssigneeId.Value),
                request.AssigneeId == null ? null : await GetUserNameAsync(request.AssigneeId.Value));
        }

        if (request.DueDate != task.DueDate)
            Log(task, ActivityType.DueDateChanged, task.DueDate?.ToString("yyyy-MM-dd"), request.DueDate?.ToString("yyyy-MM-dd"));

        task.Title = title;
        task.Description = request.Description?.Trim();
        task.Priority = request.Priority;
        task.AssigneeId = request.AssigneeId;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return await ToDetailsAsync(task, role);
    }

    public async Task<TaskCardDto> MoveAsync(int taskId, MoveTaskRequest request)
    {
        var task = await LoadAsync(taskId);
        await access.RequireProjectRoleAsync(task.ProjectId, ProjectRole.Contributor);

        var targetColumn = await db.BoardColumns
            .Where(c => c.Id == request.ColumnId && c.Board.ProjectId == task.ProjectId)
            .SingleOrDefaultAsync()
            ?? throw new BadRequestException("The target column is not part of this project.");

        var above = await GetNeighbourPositionAsync(request.AboveTaskId, targetColumn.Id);
        var below = await GetNeighbourPositionAsync(request.BelowTaskId, targetColumn.Id);

        if (TaskOrdering.NeedsRebalance(above, below))
        {
            // Positions in this column have been split so often that there is no room left
            // between the neighbours. Spread the column out evenly and use the fresh values.
            await RebalanceColumnAsync(targetColumn.Id);
            above = await GetNeighbourPositionAsync(request.AboveTaskId, targetColumn.Id);
            below = await GetNeighbourPositionAsync(request.BelowTaskId, targetColumn.Id);
        }

        if (task.ColumnId != targetColumn.Id)
        {
            var previousColumn = await db.BoardColumns.SingleAsync(c => c.Id == task.ColumnId);
            Log(task, ActivityType.StatusChanged, previousColumn.Name, targetColumn.Name);

            task.CompletedAt = targetColumn.Category switch
            {
                ColumnCategory.Done => task.CompletedAt ?? DateTime.UtcNow,
                _ => null
            };
            task.ColumnId = targetColumn.Id;
        }

        task.Position = TaskOrdering.Between(above, below);
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await db.Tasks.Where(t => t.Id == task.Id).Select(Card).SingleAsync();
    }

    public async Task DeleteAsync(int taskId)
    {
        var task = await LoadAsync(taskId);
        var role = await access.RequireProjectRoleAsync(task.ProjectId, ProjectRole.Contributor);

        // Contributors can delete tasks they reported, managers can delete any task.
        if (role < ProjectRole.Manager && task.ReporterId != currentUser.Id)
            throw new ForbiddenException("Only the person who created the task or a project manager can delete it.");

        var projectKey = await db.Projects.Where(p => p.Id == task.ProjectId).Select(p => p.Key).SingleAsync();

        // Keeps a trace in the project history; the task reference itself becomes null.
        db.ActivityLogs.Add(new ActivityLog
        {
            ProjectId = task.ProjectId,
            UserId = currentUser.Id,
            Type = ActivityType.TaskDeleted,
            NewValue = $"{projectKey}-{task.Number} {task.Title}"
        });

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
    }

    // Used here and by BoardService, so a card looks the same everywhere.
    public static Expression<Func<TaskItem, TaskCardDto>> Card { get; } =
        t => new TaskCardDto(
            t.Id,
            t.Number,
            t.Title,
            t.Priority,
            t.DueDate,
            t.ColumnId,
            t.Position,
            t.Assignee == null ? null : new MemberSummaryDto(t.Assignee.Id, t.Assignee.FullName, t.Assignee.Email),
            t.Description != null && t.Description != "",
            t.Comments.Count);

    private async Task<TaskItem> LoadAsync(int taskId) =>
        await db.Tasks.SingleOrDefaultAsync(t => t.Id == taskId)
        ?? throw new NotFoundException("Task not found.");

    private async Task<TaskDetailsDto> ToDetailsAsync(TaskItem task, ProjectRole role)
    {
        return await db.Tasks
            .Where(t => t.Id == task.Id)
            .Select(t => new TaskDetailsDto(
                t.Id,
                t.Number,
                t.ProjectId,
                t.Project.Key,
                t.Column.BoardId,
                t.ColumnId,
                t.Column.Name,
                t.Title,
                t.Description,
                t.Priority,
                t.DueDate,
                t.Assignee == null ? null : new MemberSummaryDto(t.Assignee.Id, t.Assignee.FullName, t.Assignee.Email),
                new MemberSummaryDto(t.Reporter.Id, t.Reporter.FullName, t.Reporter.Email),
                t.CreatedAt,
                t.UpdatedAt,
                t.CompletedAt,
                role,
                Convert.ToBase64String(t.RowVersion)))
            .SingleAsync();
    }

    private async Task<double?> GetNeighbourPositionAsync(int? taskId, int columnId)
    {
        if (taskId == null)
            return null;

        return await db.Tasks
            .Where(t => t.Id == taskId && t.ColumnId == columnId)
            .Select(t => (double?)t.Position)
            .SingleOrDefaultAsync();
    }

    private async Task RebalanceColumnAsync(int columnId)
    {
        var tasks = await db.Tasks.Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Position).ThenBy(t => t.Id)
            .ToListAsync();

        for (var index = 0; index < tasks.Count; index++)
        {
            tasks[index].Position = (index + 1) * TaskOrdering.Gap;
        }

        await db.SaveChangesAsync();
    }

    private async Task EnsureAssigneeIsMemberAsync(int projectId, int? assigneeId)
    {
        if (assigneeId == null)
            return;

        var isMember = await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == assigneeId)
            || await db.Projects.AnyAsync(p => p.Id == projectId
                && p.Organization.Members.Any(m => m.UserId == assigneeId && m.Role >= OrganizationRole.Admin));

        if (!isMember)
            throw new BadRequestException("Tasks can only be assigned to members of the project.");
    }

    private Task<string> GetUserNameAsync(int userId) =>
        db.Users.Where(u => u.Id == userId).Select(u => u.FullName).SingleAsync();

    private void Log(TaskItem task, ActivityType type, string? oldValue = null, string? newValue = null)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            ProjectId = task.ProjectId,
            Task = task,
            UserId = currentUser.Id,
            Type = type,
            OldValue = Trim(oldValue),
            NewValue = Trim(newValue)
        });

        static string? Trim(string? value) => value?.Length > 200 ? value[..200] : value;
    }

    // Task numbers come from a counter on the project that is a concurrency token, so two
    // tasks created at the same moment can't share a number: one save fails and is retried.
    private async Task SaveWithUniqueNumberRetryAsync(TaskItem task, Project project)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await db.SaveChangesAsync();
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 3)
            {
                await db.Entry(project).ReloadAsync();
                task.Number = ++project.TaskCounter;
            }
        }
    }
}
