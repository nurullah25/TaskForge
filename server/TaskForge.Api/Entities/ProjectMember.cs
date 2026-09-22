namespace TaskForge.Api.Entities;

public class ProjectMember
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ProjectRole Role { get; set; } = ProjectRole.Contributor;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
