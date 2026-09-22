using Microsoft.AspNetCore.Mvc;

namespace TaskForge.Api.Features.Projects;

[ApiController]
[Route("api")]
public class ProjectsController(ProjectService projects) : ControllerBase
{
    [HttpGet("organizations/{organizationId:int}/projects")]
    public Task<List<ProjectDto>> GetForOrganization(int organizationId) =>
        projects.GetForOrganizationAsync(organizationId);

    [HttpPost("organizations/{organizationId:int}/projects")]
    public async Task<ActionResult<ProjectDetailsDto>> Create(int organizationId, CreateProjectRequest request)
    {
        var project = await projects.CreateAsync(organizationId, request);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }

    [HttpGet("projects/{id:int}")]
    public Task<ProjectDetailsDto> Get(int id) => projects.GetAsync(id);

    [HttpPut("projects/{id:int}")]
    public Task<ProjectDetailsDto> Update(int id, UpdateProjectRequest request) => projects.UpdateAsync(id, request);

    [HttpDelete("projects/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await projects.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("projects/{id:int}/members")]
    public Task<List<ProjectMemberDto>> GetMembers(int id) => projects.GetMembersAsync(id);

    [HttpPost("projects/{id:int}/members")]
    public async Task<ActionResult<ProjectMemberDto>> AddMember(int id, AddProjectMemberRequest request)
    {
        var member = await projects.AddMemberAsync(id, request);
        return CreatedAtAction(nameof(GetMembers), new { id }, member);
    }

    [HttpPut("projects/{id:int}/members/{userId:int}")]
    public Task<ProjectMemberDto> UpdateMember(int id, int userId, UpdateProjectMemberRequest request) =>
        projects.UpdateMemberAsync(id, userId, request);

    [HttpDelete("projects/{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        await projects.RemoveMemberAsync(id, userId);
        return NoContent();
    }
}
