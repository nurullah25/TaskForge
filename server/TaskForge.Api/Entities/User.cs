namespace TaskForge.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrganizationMember> Organizations { get; set; } = [];
    public List<ProjectMember> Projects { get; set; } = [];
}
