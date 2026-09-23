using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskForge.Api.Common;
using TaskForge.Api.Entities;
using TaskForge.Api.Features.Activity;
using TaskForge.Api.Features.Attachments;
using TaskForge.Api.Features.Boards;
using TaskForge.Api.Features.Comments;
using TaskForge.Api.Features.Labels;
using TaskForge.Api.Features.Tasks;

namespace TaskForge.Api.Tests;

// Labels, comments, activity history and attachments.
[Collection(ApiCollection.Name)]
public class CollaborationTests(TaskForgeApiFactory factory)
{
    [Fact]
    public async Task Labels_are_unique_per_project_and_appear_on_the_cards_that_use_them()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Fix the header");

        var bug = await context.CreateLabelAsync("bug", "#ef4444");
        var duplicate = await context.Client.PostAsJsonAsync($"/api/projects/{context.ProjectId}/labels",
            new { Name = "bug", Color = "#111111" });

        await context.Client.PutAsJsonAsync($"/api/tasks/{task.Id}/labels", new { LabelIds = new[] { bug.Id } });
        var board = await context.Client.GetJsonAsync<BoardDto>($"/api/boards/{context.BoardId}");
        var card = board.Columns.SelectMany(c => c.Tasks).Single(t => t.Id == task.Id);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("bug", Assert.Single(card.Labels).Name);
    }

    [Fact]
    public async Task Labels_from_another_project_are_rejected()
    {
        var context = await CreateProjectAsync();
        var other = await context.Client.CreateProjectAsync(context.OrganizationId, "OTH");
        var otherLabel = await context.Client.PostAsJsonAsync($"/api/projects/{other.Id}/labels",
            new { Name = "urgent", Color = "#dc2626" });
        var foreign = await otherLabel.ReadJsonAsync<LabelDto>();
        var task = await context.CreateTaskAsync("Task");

        var response = await context.Client.PutAsJsonAsync($"/api/tasks/{task.Id}/labels",
            new { LabelIds = new[] { foreign.Id } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Setting_labels_replaces_the_previous_selection()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Task");
        var bug = await context.CreateLabelAsync("bug", "#ef4444");
        var frontend = await context.CreateLabelAsync("frontend", "#3b82f6");

        await context.Client.PutAsJsonAsync($"/api/tasks/{task.Id}/labels", new { LabelIds = new[] { bug.Id, frontend.Id } });
        var response = await context.Client.PutAsJsonAsync($"/api/tasks/{task.Id}/labels", new { LabelIds = new[] { frontend.Id } });
        var labels = await response.ReadJsonAsync<List<LabelDto>>();

        Assert.Equal(["frontend"], labels.Select(l => l.Name));
    }

    [Fact]
    public async Task Deleting_a_label_removes_it_from_its_tasks()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Task");
        var label = await context.CreateLabelAsync("bug", "#ef4444");
        await context.Client.PutAsJsonAsync($"/api/tasks/{task.Id}/labels", new { LabelIds = new[] { label.Id } });

        await context.Client.DeleteAsync($"/api/labels/{label.Id}");
        var details = await context.Client.GetJsonAsync<TaskDetailsDto>($"/api/tasks/{task.Id}");

        Assert.Empty(details.Labels);
    }

    [Fact]
    public async Task Viewers_can_comment_but_not_edit_someone_elses_comment()
    {
        var context = await CreateProjectAsync();
        var (viewer, viewerUser) = await factory.CreateSignedInClientAsync("Viewer Vic");
        await context.Client.AddOrganizationMemberAsync(context.OrganizationId, viewerUser.Email);
        await context.Client.AddProjectMemberAsync(context.ProjectId, viewerUser.Id, "Viewer");
        var task = await context.CreateTaskAsync("Task");

        var posted = await viewer.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { Body = "Looks good to me" });
        var comment = await posted.ReadJsonAsync<CommentDto>();
        var editByOther = await context.Client.PutAsJsonAsync($"/api/comments/{comment.Id}", new { Body = "Edited" });

        Assert.Equal(HttpStatusCode.Created, posted.StatusCode);
        Assert.Equal("Viewer Vic", comment.Author.FullName);
        Assert.Equal(HttpStatusCode.Forbidden, editByOther.StatusCode);
    }

    [Fact]
    public async Task Comments_are_paged_oldest_first_and_counted_on_the_card()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Task");

        for (var i = 1; i <= 5; i++)
        {
            await context.Client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { Body = $"Comment {i}" });
        }

        var firstPage = await context.Client.GetJsonAsync<PagedResult<CommentDto>>(
            $"/api/tasks/{task.Id}/comments?page=1&pageSize=2");
        var lastPage = await context.Client.GetJsonAsync<PagedResult<CommentDto>>(
            $"/api/tasks/{task.Id}/comments?page=3&pageSize=2");
        var board = await context.Client.GetJsonAsync<BoardDto>($"/api/boards/{context.BoardId}");

        Assert.Equal(["Comment 1", "Comment 2"], firstPage.Items.Select(c => c.Body));
        Assert.Equal(5, firstPage.TotalCount);
        Assert.True(firstPage.HasMore);
        Assert.Equal(["Comment 5"], lastPage.Items.Select(c => c.Body));
        Assert.False(lastPage.HasMore);
        Assert.Equal(5, board.Columns.SelectMany(c => c.Tasks).Single(t => t.Id == task.Id).CommentCount);
    }

    [Fact]
    public async Task Managers_can_delete_other_peoples_comments()
    {
        var context = await CreateProjectAsync();
        var (contributor, contributorUser) = await factory.CreateSignedInClientAsync();
        await context.Client.AddOrganizationMemberAsync(context.OrganizationId, contributorUser.Email);
        await context.Client.AddProjectMemberAsync(context.ProjectId, contributorUser.Id, "Contributor");
        var task = await context.CreateTaskAsync("Task");

        var posted = await contributor.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { Body = "Mine" });
        var comment = await posted.ReadJsonAsync<CommentDto>();
        var deleted = await context.Client.DeleteAsync($"/api/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Task_history_lists_what_happened_newest_first()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Original");
        await context.Client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new { Title = "Renamed", Priority = "High", RowVersion = task.RowVersion });
        await context.Client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { Body = "A comment" });

        var history = await context.Client.GetJsonAsync<List<ActivityDto>>($"/api/tasks/{task.Id}/activity");

        Assert.Equal(ActivityType.CommentAdded, history[0].Type);
        Assert.Contains(history, a => a.Type == ActivityType.TitleChanged && a.OldValue == "Original" && a.NewValue == "Renamed");
        Assert.Contains(history, a => a.Type == ActivityType.PriorityChanged && a.NewValue == "High");
        Assert.Contains(history, a => a.Type == ActivityType.TaskCreated);
    }

    [Fact]
    public async Task Project_history_is_paged_and_hidden_from_outsiders()
    {
        var context = await CreateProjectAsync();
        var (outsider, _) = await factory.CreateSignedInClientAsync();
        await context.CreateTaskAsync("Task");

        var history = await context.Client.GetJsonAsync<PagedResult<ActivityDto>>(
            $"/api/projects/{context.ProjectId}/activity?page=1&pageSize=10");
        var forbidden = await outsider.GetAsync($"/api/projects/{context.ProjectId}/activity");

        Assert.NotEmpty(history.Items);
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
    }

    [Fact]
    public async Task Attachment_can_be_uploaded_downloaded_and_deleted()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Task");

        var uploaded = await context.UploadAsync(task.Id, "notes.txt", "text/plain", "Remember the milk");
        var attachment = await uploaded.ReadJsonAsync<AttachmentDto>();
        var download = await context.Client.GetAsync($"/api/attachments/{attachment.Id}/download");
        var content = await download.Content.ReadAsStringAsync();
        var deleted = await context.Client.DeleteAsync($"/api/attachments/{attachment.Id}");
        var afterDelete = await context.Client.GetJsonAsync<List<AttachmentDto>>($"/api/tasks/{task.Id}/attachments");

        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        Assert.Equal("notes.txt", attachment.FileName);
        Assert.Equal("Remember the milk", content);
        Assert.Equal("notes.txt", download.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task Executable_files_are_rejected()
    {
        var context = await CreateProjectAsync();
        var task = await context.CreateTaskAsync("Task");

        var response = await context.UploadAsync(task.Id, "tool.exe", "application/octet-stream", "MZ...");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Viewers_cannot_upload_attachments()
    {
        var context = await CreateProjectAsync();
        var (viewer, viewerUser) = await factory.CreateSignedInClientAsync();
        await context.Client.AddOrganizationMemberAsync(context.OrganizationId, viewerUser.Email);
        await context.Client.AddProjectMemberAsync(context.ProjectId, viewerUser.Id, "Viewer");
        var task = await context.CreateTaskAsync("Task");

        var response = await UploadAsync(viewer, task.Id, "notes.txt", "text/plain", "hello");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client, int taskId, string fileName, string contentType, string content)
    {
        using var form = new MultipartFormDataContent();
        var file = new StringContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        return await client.PostAsync($"/api/tasks/{taskId}/attachments", form);
    }

    private async Task<Context> CreateProjectAsync()
    {
        var (client, _) = await factory.CreateSignedInClientAsync();
        var organization = await client.CreateOrganizationAsync();
        var project = await client.CreateProjectAsync(organization.Id);
        var board = await client.GetJsonAsync<BoardDto>($"/api/boards/{project.Boards[0].Id}");

        return new Context(client, organization.Id, project.Id, board);
    }

    private record Context(HttpClient Client, int OrganizationId, int ProjectId, BoardDto Board)
    {
        public int BoardId => Board.Id;

        public async Task<TaskDetailsDto> CreateTaskAsync(string title)
        {
            var response = await Client.PostAsJsonAsync("/api/tasks",
                new { ColumnId = Board.Columns[0].Id, Title = title });
            response.EnsureSuccessStatusCode();
            return await response.ReadJsonAsync<TaskDetailsDto>();
        }

        public async Task<LabelDto> CreateLabelAsync(string name, string color)
        {
            var response = await Client.PostAsJsonAsync($"/api/projects/{ProjectId}/labels", new { Name = name, Color = color });
            response.EnsureSuccessStatusCode();
            return await response.ReadJsonAsync<LabelDto>();
        }

        public Task<HttpResponseMessage> UploadAsync(int taskId, string fileName, string contentType, string content) =>
            CollaborationTests.UploadAsync(Client, taskId, fileName, contentType, content);
    }
}
