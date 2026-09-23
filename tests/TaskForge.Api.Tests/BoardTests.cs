using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;
using TaskForge.Api.Features.Boards;

namespace TaskForge.Api.Tests;

[Collection(ApiCollection.Name)]
public class BoardTests(TaskForgeApiFactory factory)
{
    [Fact]
    public async Task New_project_comes_with_a_board_of_four_ordered_columns()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        var project = await owner.CreateProjectAsync(organization.Id);

        var board = await owner.GetJsonAsync<BoardDto>($"/api/boards/{project.Boards[0].Id}");

        Assert.Equal(["To do", "In progress", "Testing", "Done"], board.Columns.Select(c => c.Name));
        Assert.Equal([1, 2, 3, 4], board.Columns.Select(c => c.Position));
        Assert.Equal(ColumnCategory.Done, board.Columns.Last().Category);
    }

    [Fact]
    public async Task Contributors_can_read_the_board_but_not_change_its_columns()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (contributor, contributorUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, contributorUser.Email);
        var project = await owner.CreateProjectAsync(organization.Id);
        await owner.AddProjectMemberAsync(project.Id, contributorUser.Id, "Contributor");
        var boardId = project.Boards[0].Id;

        var read = await contributor.GetAsync($"/api/boards/{boardId}");
        var addColumn = await contributor.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { Name = "Blocked", Category = "InProgress" });

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, addColumn.StatusCode);
    }

    [Fact]
    public async Task New_column_is_added_at_the_end()
    {
        var (owner, boardId) = await CreateBoardAsync();

        var response = await owner.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { Name = "Blocked", Category = "InProgress" });
        var column = await response.ReadJsonAsync<BoardColumnDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(5, column.Position);
        Assert.Equal(ColumnCategory.InProgress, column.Category);
    }

    [Fact]
    public async Task Reordering_columns_renumbers_them_from_one()
    {
        var (owner, boardId) = await CreateBoardAsync();
        var board = await owner.GetJsonAsync<BoardDto>($"/api/boards/{boardId}");
        var reversed = board.Columns.Select(c => c.Id).Reverse().ToList();

        var response = await owner.PutAsJsonAsync($"/api/boards/{boardId}/columns/order", new { ColumnIds = reversed });
        var columns = await response.ReadJsonAsync<List<BoardColumnDto>>();

        Assert.Equal(reversed, columns.Select(c => c.Id));
        Assert.Equal([1, 2, 3, 4], columns.Select(c => c.Position));
    }

    [Fact]
    public async Task Reordering_rejects_an_incomplete_list_of_columns()
    {
        var (owner, boardId) = await CreateBoardAsync();
        var board = await owner.GetJsonAsync<BoardDto>($"/api/boards/{boardId}");

        var response = await owner.PutAsJsonAsync($"/api/boards/{boardId}/columns/order",
            new { ColumnIds = board.Columns.Take(2).Select(c => c.Id) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Column_with_tasks_cannot_be_deleted()
    {
        var (owner, boardId) = await CreateBoardAsync();
        var board = await owner.GetJsonAsync<BoardDto>($"/api/boards/{boardId}");
        var column = board.Columns[0];
        await AddTaskAsync(board.ProjectId, column.Id);

        var response = await owner.DeleteAsync($"/api/columns/{column.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Empty_column_can_be_deleted_but_not_the_last_one()
    {
        var (owner, boardId) = await CreateBoardAsync();
        var board = await owner.GetJsonAsync<BoardDto>($"/api/boards/{boardId}");

        foreach (var column in board.Columns.Take(3))
        {
            Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/columns/{column.Id}")).StatusCode);
        }

        var last = await owner.DeleteAsync($"/api/columns/{board.Columns.Last().Id}");
        Assert.Equal(HttpStatusCode.Conflict, last.StatusCode);
    }

    [Fact]
    public async Task Project_keeps_at_least_one_board()
    {
        var (owner, boardId) = await CreateBoardAsync();

        var response = await owner.DeleteAsync($"/api/boards/{boardId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Extra_board_can_be_created_and_deleted()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        var project = await owner.CreateProjectAsync(organization.Id);

        var created = await owner.PostAsJsonAsync($"/api/projects/{project.Id}/boards", new { Name = "Bug triage" });
        var board = await created.ReadJsonAsync<BoardDto>();
        var boards = await owner.GetJsonAsync<List<BoardSummaryDto>>($"/api/projects/{project.Id}/boards");
        var deleted = await owner.DeleteAsync($"/api/boards/{board.Id}");

        Assert.Equal(4, board.Columns.Count);
        Assert.Equal(2, boards.Count);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    private async Task<(HttpClient Client, int BoardId)> CreateBoardAsync()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        var project = await owner.CreateProjectAsync(organization.Id);
        return (owner, project.Boards[0].Id);
    }

    // Tasks have no API yet, so they are inserted directly.
    private async Task AddTaskAsync(int projectId, int columnId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reporterId = await db.ProjectMembers.Where(m => m.ProjectId == projectId).Select(m => m.UserId).FirstAsync();

        db.Tasks.Add(new TaskItem
        {
            ProjectId = projectId,
            ColumnId = columnId,
            Number = 1,
            Title = "Test task",
            ReporterId = reporterId,
            Position = 1000
        });
        await db.SaveChangesAsync();
    }
}
