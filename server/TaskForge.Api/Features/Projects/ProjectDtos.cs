using System.ComponentModel.DataAnnotations;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Projects;

public record ProjectDto(
    int Id,
    int OrganizationId,
    string Key,
    string Name,
    string? Description,
    ProjectStatus Status,
    ProjectRole MyRole,
    int MemberCount,
    int OpenTaskCount,
    DateTime CreatedAt);

public record ProjectBoardDto(int Id, string Name);

public record ProjectDetailsDto(
    int Id,
    int OrganizationId,
    string OrganizationName,
    string Key,
    string Name,
    string? Description,
    ProjectStatus Status,
    ProjectRole MyRole,
    bool CanDelete,
    DateTime CreatedAt,
    List<ProjectBoardDto> Boards);

public record ProjectMemberDto(int UserId, string FullName, string Email, ProjectRole Role, DateTime AddedAt);

public class CreateProjectRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    // Becomes the task key prefix (CP-12), so it's short and can't change later.
    [Required, RegularExpression("^[A-Za-z][A-Za-z0-9]{1,9}$",
        ErrorMessage = "Key must be 2-10 letters or digits and start with a letter.")]
    public string Key { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public class UpdateProjectRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    [EnumDataType(typeof(ProjectStatus))]
    public ProjectStatus Status { get; set; }
}

public class AddProjectMemberRequest
{
    public int UserId { get; set; }

    [EnumDataType(typeof(ProjectRole))]
    public ProjectRole Role { get; set; } = ProjectRole.Contributor;
}

public class UpdateProjectMemberRequest
{
    [EnumDataType(typeof(ProjectRole))]
    public ProjectRole Role { get; set; }
}
