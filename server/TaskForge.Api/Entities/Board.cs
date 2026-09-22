namespace TaskForge.Api.Entities;

public class Board
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<BoardColumn> Columns { get; set; } = [];

    public static Board CreateWithDefaultColumns(string name) => new()
    {
        Name = name,
        Columns =
        [
            new BoardColumn { Name = "To do", Position = 1, Category = ColumnCategory.ToDo },
            new BoardColumn { Name = "In progress", Position = 2, Category = ColumnCategory.InProgress },
            new BoardColumn { Name = "Testing", Position = 3, Category = ColumnCategory.InProgress },
            new BoardColumn { Name = "Done", Position = 4, Category = ColumnCategory.Done }
        ]
    };
}
