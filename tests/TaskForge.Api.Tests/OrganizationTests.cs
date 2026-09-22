using System.Net;
using System.Net.Http.Json;
using TaskForge.Api.Entities;
using TaskForge.Api.Features.Organizations;

namespace TaskForge.Api.Tests;

[Collection(ApiCollection.Name)]
public class OrganizationTests(TaskForgeApiFactory factory)
{
    [Fact]
    public async Task Creator_becomes_owner_and_sees_the_organization_in_their_list()
    {
        var (client, _) = await factory.CreateSignedInClientAsync();

        var created = await client.CreateOrganizationAsync("Northwind");
        var mine = await client.GetJsonAsync<List<OrganizationDto>>("/api/organizations");

        Assert.Equal(OrganizationRole.Owner, created.MyRole);
        var listed = Assert.Single(mine);
        Assert.Equal("Northwind", listed.Name);
    }

    [Fact]
    public async Task Non_members_get_not_found_instead_of_forbidden()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (outsider, _) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();

        var response = await outsider.GetAsync($"/api/organizations/{organization.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Regular_members_cannot_add_members()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (member, memberUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, memberUser.Email);

        var response = await member.PostAsJsonAsync($"/api/organizations/{organization.Id}/members",
            new { Email = TaskForgeApiFactory.UniqueEmail(), Role = "Member" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Adding_members_rejects_unknown_emails_and_duplicates()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (_, existingUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        var url = $"/api/organizations/{organization.Id}/members";

        var unknown = await owner.PostAsJsonAsync(url, new { Email = TaskForgeApiFactory.UniqueEmail(), Role = "Member" });
        var first = await owner.PostAsJsonAsync(url, new { Email = existingUser.Email, Role = "Member" });
        var duplicate = await owner.PostAsJsonAsync(url, new { Email = existingUser.Email, Role = "Member" });

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Admins_cannot_promote_anyone_to_owner()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (admin, adminUser) = await factory.CreateSignedInClientAsync();
        var (_, memberUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, adminUser.Email, "Admin");
        await owner.AddOrganizationMemberAsync(organization.Id, memberUser.Email);

        var response = await admin.PutAsJsonAsync(
            $"/api/organizations/{organization.Id}/members/{memberUser.Id}", new { Role = "Owner" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Last_owner_cannot_leave_or_be_demoted()
    {
        var (owner, ownerUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        var memberUrl = $"/api/organizations/{organization.Id}/members/{ownerUser.Id}";

        var leave = await owner.DeleteAsync(memberUrl);
        var demote = await owner.PutAsJsonAsync(memberUrl, new { Role = "Admin" });

        Assert.Equal(HttpStatusCode.Conflict, leave.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);
    }

    [Fact]
    public async Task Removing_a_member_also_removes_them_from_the_organizations_projects()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var (member, memberUser) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.AddOrganizationMemberAsync(organization.Id, memberUser.Email);
        var project = await owner.CreateProjectAsync(organization.Id);
        await owner.AddProjectMemberAsync(project.Id, memberUser.Id, "Contributor");

        var removal = await owner.DeleteAsync($"/api/organizations/{organization.Id}/members/{memberUser.Id}");
        var projectMembers = await owner.GetJsonAsync<List<object>>($"/api/projects/{project.Id}/members");

        Assert.Equal(HttpStatusCode.NoContent, removal.StatusCode);
        Assert.Single(projectMembers);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/projects/{project.Id}")).StatusCode);
    }

    [Fact]
    public async Task Organization_with_projects_cannot_be_deleted()
    {
        var (owner, _) = await factory.CreateSignedInClientAsync();
        var organization = await owner.CreateOrganizationAsync();
        await owner.CreateProjectAsync(organization.Id);

        var response = await owner.DeleteAsync($"/api/organizations/{organization.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
