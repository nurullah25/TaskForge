using Microsoft.AspNetCore.Mvc;

namespace TaskForge.Api.Features.Organizations;

[ApiController]
[Route("api/organizations")]
public class OrganizationsController(OrganizationService organizations) : ControllerBase
{
    [HttpGet]
    public Task<List<OrganizationDto>> GetMine() => organizations.GetMineAsync();

    [HttpGet("{id:int}")]
    public Task<OrganizationDto> Get(int id) => organizations.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> Create(SaveOrganizationRequest request)
    {
        var organization = await organizations.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = organization.Id }, organization);
    }

    [HttpPut("{id:int}")]
    public Task<OrganizationDto> Update(int id, SaveOrganizationRequest request) => organizations.UpdateAsync(id, request);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await organizations.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/members")]
    public Task<List<OrganizationMemberDto>> GetMembers(int id) => organizations.GetMembersAsync(id);

    [HttpPost("{id:int}/members")]
    public async Task<ActionResult<OrganizationMemberDto>> AddMember(int id, AddOrganizationMemberRequest request)
    {
        var member = await organizations.AddMemberAsync(id, request);
        return CreatedAtAction(nameof(GetMembers), new { id }, member);
    }

    [HttpPut("{id:int}/members/{userId:int}")]
    public Task<OrganizationMemberDto> UpdateMember(int id, int userId, UpdateOrganizationMemberRequest request) =>
        organizations.UpdateMemberAsync(id, userId, request);

    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        await organizations.RemoveMemberAsync(id, userId);
        return NoContent();
    }
}
