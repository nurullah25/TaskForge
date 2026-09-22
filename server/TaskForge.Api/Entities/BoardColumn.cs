namespace TaskForge.Api.Entities;

public class BoardColumn
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public string Name { get; set; } = "";
    public int Position { get; set; }

    // Columns can be renamed freely ("QA", "Shipped"...), the category is what
    // reports and the dashboard use to decide whether a task is open or done.
    public ColumnCategory Category { get; set; } = ColumnCategory.ToDo;

    public List<TaskItem> Tasks { get; set; } = [];
}
