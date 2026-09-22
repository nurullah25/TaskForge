using System.ComponentModel.DataAnnotations;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Organizations;

public record OrganizationDto(int Id, string Name, OrganizationRole MyRole, int MemberCount, int ProjectCount);

public record OrganizationMemberDto(int UserId, string FullName, string Email, OrganizationRole Role, DateTime JoinedAt);

public class SaveOrganizationRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
}

public class AddOrganizationMemberRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [EnumDataType(typeof(OrganizationRole))]
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
}

public class UpdateOrganizationMemberRequest
{
    [EnumDataType(typeof(OrganizationRole))]
    public OrganizationRole Role { get; set; }
}
