using System.Net;
using System.Net.Http.Json;
using TaskForge.Api.Common;
using TaskForge.Api.Features.Boards;
using TaskForge.Api.Features.Dashboard;
using TaskForge.Api.Features.Labels;
using TaskForge.Api.Features.Tasks;

namespace TaskForge.Api.Tests;

[Collection(ApiCollection.Name)]
public class SearchAndDashboardTests(TaskForgeApiFactory factory)
{
    [Fact]
    public async Task Tasks_can_be_filtered_by_assignee_priority_and_text()
    {
        var context = await CreateProjectAsync();
        await context.CreateTaskAsync("Build the login page", priority: "High", assigneeId: context.UserId);
        await context.CreateTaskAsync("Write the release notes", priority: "Low");
        await context.CreateTaskAsync("Fix login redirect", priority: "High");

        var mine = await context.SearchAsync($"assigneeId={context.UserId}");
        var high = await context.SearchAsync("priority=High");
        var unassigned = await context.SearchAsync("unassigned=true");
        var text = await context.SearchAsync("text=login");

        Assert.Equal(["Build the login page"], mine.Items.Select(t => t.Title));
        Assert.Equal(2, high.TotalCount);
        Assert.Equal(2, unassigned.TotalCount);
        Assert.Equal(["Fix login redirect", "Build the login page"], text.Items.Select(t => t.Title));
    }

    [Fact]
    public async Task Tasks_can_be_filtered_by_label_status_and_due_date()
    {
        var context = await CreateProjectAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var bug = await context.CreateLabelAsync("bug");

        var labelled = await context.CreateTaskAsync("Crash on save", dueDate: today.AddDays(-3));
        await context.Client.PutAsJsonAsync($"/api/tasks/{labelled.Id}/labels", new { LabelIds = new[] { bug.Id } });
        await context.CreateTaskAsync("Later work", dueDate: today.AddDays(10));
        var done = await context.CreateTaskAsync("Already finished", dueDate: today.AddDays(-5));
        await context.Client.PostAsJsonAsync($"/api/tasks/{done.Id}/move", new { ColumnId = context.DoneColumnId });

        var byLabel = await context.SearchAsync($"labelId={bug.Id}");
        var overdue = await context.SearchAsync("overdue=true");
        var finished = await context.SearchAsync("category=Done");
        var dueWindow = await context.SearchAsync($"dueFrom={today:yyyy-MM-dd}&dueTo={today.AddDays(30):yyyy-MM-dd}");

        Assert.Equal(["Crash on save"], byLabel.Items.Select(t => t.Title));
        // The finished task is past its due date but no longer open, so it isn't overdue.
        Assert.Equal(["Crash on save"], overdue.Items.Select(t => t.Title));
        Assert.Equal(["Already finished"], finished.Items.Select(t => t.Title));
        Assert.Equal(["Later work"], dueWindow.Items.Select(t => t.Title));
    }

    [Fact]
    public async Task Results_are_paged_and_can_be_sorted_by_due_date()
    {
        var context = await CreateProjectAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await context.CreateTaskAsync("No due date");
        await context.CreateTaskAsync("Due later", dueDate: today.AddDays(5));
        await context.CreateTaskAsync("Due soon", dueDate: today.AddDays(1));

        var sorted = await context.SearchAsync("sort=DueDate");
        var firstPage = await context.SearchAsync("sort=DueDate&page=1&pageSize=2");

        Assert.Equal(["Due soon", "Due later", "No due date"], sorted.Items.Select(t => t.Title));
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasMore);
    }

    [Fact]
    public async Task Search_is_closed_to_people_outside_the_project()
    {
        var context = await CreateProjectAsync();
        var (outsider, _) = await factory.CreateSignedInClientAsync();

        var response = await outsider.GetAsync($"/api/projects/{context.ProjectId}/tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_counts_the_work_across_visible_projects()
    {
        var context = await CreateProjectAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await context.CreateTaskAsync("Open work", priority: "High", assigneeId: context.UserId);
        await context.CreateTaskAsync("Overdue work", dueDate: today.AddDays(-2), assigneeId: context.UserId);
        var finished = await context.CreateTaskAsync("Finished work");
        await context.Client.PostAsJsonAsync($"/api/tasks/{finished.Id}/move", new { ColumnId = context.DoneColumnId });

        var dashboard = await context.Client.GetJsonAsync<DashboardDto>(
            $"/api/dashboard?organizationId={context.OrganizationId}");

        Assert.Equal(1, dashboard.ProjectCount);
        Assert.Equal(2, dashboard.OpenTaskCount);
        Assert.Equal(1, dashboard.CompletedTaskCount);
        Assert.Equal(1, dashboard.OverdueTaskCount);
        Assert.Equal(2, dashboard.MyOpenTaskCount);
        Assert.Equal(["Overdue work", "Open work"], dashboard.MyTasks.Select(t => t.Title));
        Assert.Equal(2, dashboard.OpenByCategory.Single(c => c.Name == "ToDo").Count);
        Assert.Equal(["High", "Medium"], dashboard.OpenByPriority.Select(p => p.Name));
    }

    [Fact]
    public async Task Members_only_see_their_own_projects_on_the_dashboard()
    {
        var context = await CreateProjectAsync();
        var (member, memberUser) = await factory.CreateSignedInClientAsync();
        await context.Client.AddOrganizationMemberAsync(context.OrganizationId, memberUser.Email);
        await context.CreateTaskAsync("Hidden work");

        var dashboard = await member.GetJsonAsync<DashboardDto>(
            $"/api/dashboard?organizationId={context.OrganizationId}");

        Assert.Equal(0, dashboard.ProjectCount);
        Assert.Equal(0, dashboard.OpenTaskCount);
        Assert.Empty(dashboard.MyTasks);
    }

    private async Task<Context> CreateProjectAsync()
    {
        var (client, user) = await factory.CreateSignedInClientAsync();
        var organization = await client.CreateOrganizationAsync();
        var project = await client.CreateProjectAsync(organization.Id);
        var board = await client.GetJsonAsync<BoardDto>($"/api/boards/{project.Boards[0].Id}");

        return new Context(client, user.Id, organization.Id, project.Id, board);
    }

    private record Context(HttpClient Client, int UserId, int OrganizationId, int ProjectId, BoardDto Board)
    {
        public int DoneColumnId => Board.Columns.Last().Id;

        public async Task<TaskDetailsDto> CreateTaskAsync(
            string title, string priority = "Medium", int? assigneeId = null, DateOnly? dueDate = null)
        {
            var response = await Client.PostAsJsonAsync("/api/tasks", new
            {
                ColumnId = Board.Columns[0].Id,
                Title = title,
                Priority = priority,
                AssigneeId = assigneeId,
                DueDate = dueDate?.ToString("yyyy-MM-dd")
            });
            response.EnsureSuccessStatusCode();
            return await response.ReadJsonAsync<TaskDetailsDto>();
        }

        public async Task<LabelDto> CreateLabelAsync(string name)
        {
            var response = await Client.PostAsJsonAsync($"/api/projects/{ProjectId}/labels",
                new { Name = name, Color = "#ef4444" });
            response.EnsureSuccessStatusCode();
            return await response.ReadJsonAsync<LabelDto>();
        }

        public Task<PagedResult<TaskListItemDto>> SearchAsync(string filters) =>
            Client.GetJsonAsync<PagedResult<TaskListItemDto>>($"/api/projects/{ProjectId}/tasks?{filters}");
    }
}
