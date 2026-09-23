using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TaskForge.Api.Features.Boards;
using TaskForge.Api.Features.Notifications;
using TaskForge.Api.Features.Tasks;

namespace TaskForge.Api.Tests;

// These run the hub for real over the in-memory test server.
[Collection(ApiCollection.Name)]
public class RealtimeTests(TaskForgeApiFactory factory)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Board_viewers_are_told_when_someone_else_moves_a_task()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (watcher, watcherUser, watcherToken) = await factory.CreateSignedInUserAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, watcherUser.Email);
        var project = await owner.CreateProjectAsync(organization.Id);
        await owner.AddProjectMemberAsync(project.Id, watcherUser.Id, "Contributor");

        var board = await watcher.GetJsonAsync<BoardDto>($"/api/boards/{project.Boards[0].Id}");
        var created = await owner.PostAsJsonAsync("/api/tasks",
            new { ColumnId = board.Columns[0].Id, Title = "Watch me move" });
        var task = await created.ReadJsonAsync<TaskDetailsDto>();

        await using var connection = factory.CreateHubConnection(watcherToken);
        var moved = new TaskCompletionSource<TaskCardDto>();
        connection.On<TaskCardDto>("TaskMoved", card => moved.TrySetResult(card));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", board.Id);

        await owner.PostAsJsonAsync($"/api/tasks/{task.Id}/move", new { ColumnId = board.Columns[1].Id });

        var card = await moved.Task.WaitAsync(Timeout);
        Assert.Equal(task.Id, card.Id);
        Assert.Equal(board.Columns[1].Id, card.ColumnId);
    }

    [Fact]
    public async Task The_browser_that_made_the_change_is_not_told_about_its_own_move()
    {
        var (owner, _, ownerToken) = await factory.CreateSignedInUserAsync();
        var organization = await owner.CreateOrganizationAsync();
        var project = await owner.CreateProjectAsync(organization.Id);
        var board = await owner.GetJsonAsync<BoardDto>($"/api/boards/{project.Boards[0].Id}");
        var created = await owner.PostAsJsonAsync("/api/tasks",
            new { ColumnId = board.Columns[0].Id, Title = "My own move" });
        var task = await created.ReadJsonAsync<TaskDetailsDto>();

        await using var connection = factory.CreateHubConnection(ownerToken);
        var events = 0;
        connection.On<TaskCardDto>("TaskMoved", _ => Interlocked.Increment(ref events));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", board.Id);

        // The client sends its connection id with the request, exactly like the Angular app.
        owner.DefaultRequestHeaders.Add("X-Connection-Id", connection.ConnectionId!);
        await owner.PostAsJsonAsync($"/api/tasks/{task.Id}/move", new { ColumnId = board.Columns[1].Id });
        await Task.Delay(500);

        Assert.Equal(0, events);
    }

    [Fact]
    public async Task Outsiders_cannot_join_a_board_group()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (_, _, outsiderToken) = await factory.CreateSignedInUserAsync();
        var organization = await owner.CreateOrganizationAsync();
        var project = await owner.CreateProjectAsync(organization.Id);

        await using var connection = factory.CreateHubConnection(outsiderToken);
        await connection.StartAsync();

        var error = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync("JoinBoard", project.Boards[0].Id));

        Assert.Contains("access", error.Message);
    }

    [Fact]
    public async Task Assigning_a_task_notifies_the_assignee_live()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync("Sarah Khan");
        var (assignee, assigneeUser, assigneeToken) = await factory.CreateSignedInUserAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, assigneeUser.Email);
        var project = await owner.CreateProjectAsync(organization.Id);
        await owner.AddProjectMemberAsync(project.Id, assigneeUser.Id, "Contributor");
        var board = await owner.GetJsonAsync<BoardDto>($"/api/boards/{project.Boards[0].Id}");

        await using var connection = factory.CreateHubConnection(assigneeToken);
        var received = new TaskCompletionSource<NotificationDto>();
        connection.On<NotificationDto>("NotificationReceived", n => received.TrySetResult(n));
        await connection.StartAsync();

        await owner.PostAsJsonAsync("/api/tasks",
            new { ColumnId = board.Columns[0].Id, Title = "Please look at this", AssigneeId = assigneeUser.Id });

        var notification = await received.Task.WaitAsync(Timeout);
        var unread = await assignee.GetJsonAsync<int>("/api/notifications/unread-count");

        Assert.Contains("Sarah Khan assigned you", notification.Message);
        Assert.Equal(board.Id, notification.BoardId);

        // One for being added to the project, one for the assignment.
        Assert.Equal(2, unread);
    }

    [Fact]
    public async Task Notifications_can_be_listed_and_marked_as_read()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (member, memberUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, memberUser.Email);
        var project = await owner.CreateProjectAsync(organization.Id);

        // Being added to a project is itself a notification.
        await owner.AddProjectMemberAsync(project.Id, memberUser.Id, "Contributor");

        var before = await member.GetJsonAsync<int>("/api/notifications/unread-count");
        await member.PostAsync("/api/notifications/read-all", null);
        var after = await member.GetJsonAsync<int>("/api/notifications/unread-count");
        var all = await member.GetJsonAsync<Api.Common.PagedResult<NotificationDto>>("/api/notifications");

        Assert.Equal(1, before);
        Assert.Equal(0, after);
        Assert.Contains("added you to", all.Items.Single().Message);
    }

    [Fact]
    public async Task People_are_not_notified_about_their_own_actions()
    {
        var (owner, ownerUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        var project = await owner.CreateProjectAsync(organization.Id);
        var board = await owner.GetJsonAsync<BoardDto>($"/api/boards/{project.Boards[0].Id}");

        await owner.PostAsJsonAsync("/api/tasks",
            new { ColumnId = board.Columns[0].Id, Title = "Mine", AssigneeId = ownerUser.Id });

        Assert.Equal(0, await owner.GetJsonAsync<int>("/api/notifications/unread-count"));
    }
}
