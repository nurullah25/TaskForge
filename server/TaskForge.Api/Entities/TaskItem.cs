namespace TaskForge.Api.Entities;

// Named TaskItem to avoid clashing with System.Threading.Tasks.Task. The table is still "Tasks".
public class TaskItem
{
    public int Id { get; set; }

    // Also reachable through Column -> Board -> Project, but stored directly so filtering,
    // permission checks and dashboard queries don't need three joins.
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int ColumnId { get; set; }
    public BoardColumn Column { get; set; } = null!;

    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public int ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public DateOnly? DueDate { get; set; }

    // Sort order inside the column. A moved task gets the midpoint between its new
    // neighbours, so a move only updates one row.
    public double Position { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = [];

    public List<TaskLabel> Labels { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
    public List<Attachment> Attachments { get; set; } = [];
}
