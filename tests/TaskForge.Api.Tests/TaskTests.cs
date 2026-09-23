using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;
using TaskForge.Api.Features.Boards;
using TaskForge.Api.Features.Projects;
using TaskForge.Api.Features.Tasks;

namespace TaskForge.Api.Tests;

[Collection(ApiCollection.Name)]
public class TaskTests(TaskForgeApiFactory factory)
{
    [Fact]
    public async Task Tasks_are_numbered_per_project_and_start_in_the_chosen_column()
    {
        var context = await CreateProjectAsync();

        var first = await context.CreateTaskAsync("Build login page");
        var second = await context.CreateTaskAsync("Create reports");

        Assert.Equal(1, first.Number);
        Assert.Equal(2, second.Number);
        Assert.Equal(context.ToDoColumnId, first.ColumnId);
        Assert.True(await context.PositionAsync(second.Id) > await context.PositionAsync(first.Id),
            "a new task is appended below the previous one");
    }

    [Fact]
    public async Task Board_returns_its_tasks_ordered_by_position()
    {
        var context = await CreateProjectAsync();
        await context.CreateTaskAsync("First");
        await context.CreateTaskAsync("Second");

        var board = await context.Client.GetJsonAsync<BoardDto>($"/api/boards/{context.BoardId}");

        var column = board.Columns.Single(c => c.Id == context.ToDoColumnId);
        Assert.Equal(["First", "Second"], column.Tasks.Select(t => t.Title));
    }

    [Fact]
    public async Task Viewers_cannot_create_tasks()
    {
        var context = await CreateProjectAsync();
        var (viewer, viewerUser) = await factory.CreateSignedInClientAsync();
        await context.Client.AddOrganizationMemberAsync(context.OrganizationId, viewerUser.Email);
        await context.Client.AddProjectMemberAsync(context.ProjectId, viewerUser.Id, "Viewer");

        var response = await viewer.PostAsJsonAsync("/api/tasks",
            new { ColumnId = context.ToDoColumnId, Title = "Nope" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Tasks_can_only_be_assigned_to_project_members()
    {
        var context = await CreateProjectAsync();
        var (_, outsider) = await factory.CreateSignedInClientAsync();

        var response = await context.Client.PostAsJsonAsync("/api/tasks",
            new { ColumnId = context.ToDoColumnId, Title = "Task", AssigneeId = outsider.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Editing_with_an_outdated_row_version_returns_conflict()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Original title");

        var firstEdit = await context.Client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new { Title = "Changed by the first tab", Priority = "High", RowVersion = task.RowVersion });
        var secondEdit = await context.Client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new { Title = "Changed by the second tab", Priority = "Low", RowVersion = task.RowVersion });

        Assert.Equal(HttpStatusCode.OK, firstEdit.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondEdit.StatusCode);
    }

    [Fact]
    public async Task Moving_a_task_between_two_others_puts_it_in_the_middle()
    {
        var context = await CreateProjectAsync();
        var first = await context.CreateTaskAsync("First");
        var second = await context.CreateTaskAsync("Second");
        var moved = await context.CreateTaskAsync("Moved");

        var response = await context.Client.PostAsJsonAsync($"/api/tasks/{moved.Id}/move",
            new { ColumnId = context.ToDoColumnId, AboveTaskId = first.Id, BelowTaskId = second.Id });
        var card = await response.ReadJsonAsync<TaskCardDto>();

        var board = await context.Client.GetJsonAsync<BoardDto>($"/api/boards/{context.BoardId}");
        var titles = board.Columns.Single(c => c.Id == context.ToDoColumnId).Tasks.Select(t => t.Title);

        Assert.Equal(["First", "Moved", "Second"], titles);
        Assert.InRange(card.Position, await context.PositionAsync(first.Id), await context.PositionAsync(second.Id));
    }

    [Fact]
    public async Task Moving_a_task_to_a_done_column_completes_it_and_records_the_change()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Finish me");

        await context.Client.PostAsJsonAsync($"/api/tasks/{task.Id}/move", new { ColumnId = context.DoneColumnId });
        var completedAt = await context.ReadTaskAsync(task.Id, t => t.CompletedAt);

        // Moving it back out makes it open again.
        await context.Client.PostAsJsonAsync($"/api/tasks/{task.Id}/move", new { ColumnId = context.ToDoColumnId });
        var reopened = await context.ReadTaskAsync(task.Id, t => t.CompletedAt);

        Assert.NotNull(completedAt);
        Assert.Null(reopened);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var changes = await db.ActivityLogs
            .Where(a => a.TaskId == task.Id && a.Type == ActivityType.StatusChanged)
            .Select(a => a.OldValue + " -> " + a.NewValue)
            .ToListAsync();

        Assert.Equal(["To do -> Done", "Done -> To do"], changes);
    }

    [Fact]
    public async Task Positions_survive_many_drops_into_the_same_spot()
    {
        var context = await CreateProjectAsync();
        var first = await context.CreateTaskAsync("First");
        var second = await context.CreateTaskAsync("Second");

        // Every task is dropped directly under "First", so the gap halves each time
        // until the service has to spread the column out again.
        var below = second.Id;
        for (var i = 0; i < 60; i++)
        {
            var task = await context.CreateTaskAsync($"Task {i}");
            var response = await context.Client.PostAsJsonAsync($"/api/tasks/{task.Id}/move",
                new { ColumnId = context.ToDoColumnId, AboveTaskId = first.Id, BelowTaskId = below });
            response.EnsureSuccessStatusCode();
            below = task.Id;
        }

        var board = await context.Client.GetJsonAsync<BoardDto>($"/api/boards/{context.BoardId}");
        var tasks = board.Columns.Single(c => c.Id == context.ToDoColumnId).Tasks;

        Assert.Equal("First", tasks.First().Title);
        Assert.Equal("Second", tasks.Last().Title);
        Assert.Equal(tasks.Count, tasks.Select(t => t.Position).Distinct().Count());
        Assert.Equal(tasks.Select(t => t.Position).Order(), tasks.Select(t => t.Position));
    }

    [Fact]
    public async Task Tasks_cannot_be_moved_into_another_projects_column()
    {
        var context = await CreateProjectAsync();
        var otherProject = await context.Client.CreateProjectAsync(context.OrganizationId, "OTH");
        var otherBoard = await context.Client.GetJsonAsync<BoardDto>($"/api/boards/{otherProject.Boards[0].Id}");
        var task = await context.CreateTaskAsync("Task");

        var response = await context.Client.PostAsJsonAsync($"/api/tasks/{task.Id}/move",
            new { ColumnId = otherBoard.Columns[0].Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Contributors_can_only_delete_tasks_they_created()
    {
        var context = await CreateProjectAsync();
        var (contributor, contributorUser) = await factory.CreateSignedInClientAsync();
        await context.Client.AddOrganizationMemberAsync(context.OrganizationId, contributorUser.Email);
        await context.Client.AddProjectMemberAsync(context.ProjectId, contributorUser.Id, "Contributor");
        var managersTask = await context.CreateTaskAsync("Created by the manager");

        var forbidden = await contributor.DeleteAsync($"/api/tasks/{managersTask.Id}");

        var own = await contributor.PostAsJsonAsync("/api/tasks",
            new { ColumnId = context.ToDoColumnId, Title = "My own task" });
        var ownTask = await own.ReadJsonAsync<TaskDetailsDto>();
        var allowed = await contributor.DeleteAsync($"/api/tasks/{ownTask.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_task_keeps_a_line_in_the_project_history()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Short lived");

        await context.Client.DeleteAsync($"/api/tasks/{task.Id}");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = await db.ActivityLogs.SingleAsync(a =>
            a.ProjectId == context.ProjectId && a.Type == ActivityType.TaskDeleted);

        Assert.Null(entry.TaskId);
        Assert.Contains("Short lived", entry.NewValue);
        Assert.False(await db.Tasks.AnyAsync(t => t.Id == task.Id));
    }

    private async Task<ProjectContext> CreateProjectAsync()
    {
        var (client, user) = await factory.CreateSignedInClientAsync();
        var organization = await client.CreateOrganizationAsync();
        var project = await client.CreateProjectAsync(organization.Id);
        var board = await client.GetJsonAsync<BoardDto>($"/api/boards/{project.Boards[0].Id}");

        return new ProjectContext(factory, client, user.Id, organization.Id, project, board);
    }

    private record ProjectContext(
        TaskForgeApiFactory Factory,
        HttpClient Client,
        int UserId,
        int OrganizationId,
        ProjectDetailsDto Project,
        BoardDto Board)
    {
        public int ProjectId => Project.Id;
        public int BoardId => Board.Id;
        public int ToDoColumnId => Board.Columns[0].Id;
        public int DoneColumnId => Board.Columns.Last().Id;

        public async Task<TaskDetailsDto> CreateTaskAsync(string title, int? columnId = null)
        {
            var response = await Client.PostAsJsonAsync("/api/tasks",
                new { ColumnId = columnId ?? ToDoColumnId, Title = title });
            response.EnsureSuccessStatusCode();
            return await response.ReadJsonAsync<TaskDetailsDto>();
        }

        public Task<double> PositionAsync(int taskId) => ReadTaskAsync(taskId, t => t.Position);

        public async Task<T> ReadTaskAsync<T>(int taskId, Func<TaskItem, T> select)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return select(await db.Tasks.AsNoTracking().SingleAsync(t => t.Id == taskId));
        }
    }
}
