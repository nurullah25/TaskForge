using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;
using TaskForge.Api.Features.Projects;

namespace TaskForge.Api.Tests;

[Collection(ApiCollection.Name)]
public class ProjectTests(TaskForgeApiFactory factory)
{
    [Fact]
    public async Task New_project_gets_uppercase_key_creator_as_manager_and_a_default_board()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();

        var project = await owner.CreateProjectAsync(organization.Id, "web");

        Assert.Equal("WEB", project.Key);
        Assert.Equal(ProjectRole.Manager, project.MyRole);
        Assert.Equal("Main board", Assert.Single(project.Boards).Name);
    }

    [Fact]
    public async Task Project_keys_are_unique_within_an_organization()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.CreateProjectAsync(organization.Id, "API");

        var response = await owner.PostAsJsonAsync($"/api/organizations/{organization.Id}/projects",
            new { Name = "Another", Key = "api" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Regular_organization_members_only_see_projects_they_belong_to()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (member, memberUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, memberUser.Email);
        var visible = await owner.CreateProjectAsync(organization.Id, "VIS");
        var hidden = await owner.CreateProjectAsync(organization.Id, "HID");
        await owner.AddProjectMemberAsync(visible.Id, memberUser.Id, "Viewer");

        var projects = await member.GetJsonAsync<List<ProjectDto>>($"/api/organizations/{organization.Id}/projects");

        var project = Assert.Single(projects);
        Assert.Equal("VIS", project.Key);
        Assert.Equal(ProjectRole.Viewer, project.MyRole);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/projects/{hidden.Id}")).StatusCode);
    }

    [Fact]
    public async Task Viewers_can_read_but_not_change_the_project()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (viewer, viewerUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, viewerUser.Email);
        var project = await owner.CreateProjectAsync(organization.Id);
        await owner.AddProjectMemberAsync(project.Id, viewerUser.Id, "Viewer");

        var read = await viewer.GetAsync($"/api/projects/{project.Id}");
        var update = await viewer.PutAsJsonAsync($"/api/projects/{project.Id}",
            new { Name = "Renamed", Status = "Active" });

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task Organization_admins_manage_projects_without_being_project_members()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (admin, adminUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, adminUser.Email, "Admin");
        var project = await owner.CreateProjectAsync(organization.Id);

        var update = await admin.PutAsJsonAsync($"/api/projects/{project.Id}",
            new { Name = "Renamed by admin", Status = "OnHold" });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.ReadJsonAsync<ProjectDetailsDto>();
        Assert.Equal(ProjectStatus.OnHold, updated.Status);
    }

    [Fact]
    public async Task Only_organization_members_can_be_added_to_a_project()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (_, outsider) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        var project = await owner.CreateProjectAsync(organization.Id);

        var response = await owner.PostAsJsonAsync($"/api/projects/{project.Id}/members",
            new { UserId = outsider.Id, Role = "Contributor" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Project_managers_who_are_not_organization_admins_cannot_delete_the_project()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (manager, managerUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, managerUser.Email);
        var project = await owner.CreateProjectAsync(organization.Id);
        await owner.AddProjectMemberAsync(project.Id, managerUser.Id, "Manager");

        var response = await manager.DeleteAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_project_removes_its_tasks_and_history()
    {
        var (owner, ownerUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        var project = await owner.CreateProjectAsync(organization.Id);

        // There is no task API yet, so a task is inserted directly.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var column = await db.BoardColumns.FirstAsync(c => c.Board.ProjectId == project.Id);
            var task = new TaskItem { ProjectId = project.Id, ColumnId = column.Id, Number = 1, Title = "Test", ReporterId = ownerUser.Id };
            db.Tasks.Add(task);
            db.ActivityLogs.Add(new ActivityLog { ProjectId = project.Id, Task = task, UserId = ownerUser.Id, Type = ActivityType.TaskCreated });
            await db.SaveChangesAsync();
        }

        var response = await owner.DeleteAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Tasks.AnyAsync(t => t.ProjectId == project.Id));
            Assert.False(await db.Boards.AnyAsync(b => b.ProjectId == project.Id));
        }
    }
}
