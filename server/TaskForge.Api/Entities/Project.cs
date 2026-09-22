namespace TaskForge.Api.Entities;

public class Project
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // Short uppercase prefix used in task keys, e.g. "CP" in CP-12.
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    // Last task number handed out. Configured as a concurrency token so two tasks
    // created at the same moment can't get the same number.
    public int TaskCounter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ProjectMember> Members { get; set; } = [];
    public List<Board> Boards { get; set; } = [];
    public List<Label> Labels { get; set; } = [];
}
