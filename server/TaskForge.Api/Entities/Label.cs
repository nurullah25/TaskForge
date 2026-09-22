namespace TaskForge.Api.Entities;

public class Label
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Name { get; set; } = "";
    public string Color { get; set; } = "#64748b";

    public List<TaskLabel> Tasks { get; set; } = [];
}
