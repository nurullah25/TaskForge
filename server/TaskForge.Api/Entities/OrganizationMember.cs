namespace TaskForge.Api.Entities;

public class OrganizationMember
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
