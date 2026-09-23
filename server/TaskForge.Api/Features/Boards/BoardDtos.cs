using System.ComponentModel.DataAnnotations;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Boards;

public record BoardColumnDto(int Id, string Name, int Position, ColumnCategory Category);

public record BoardDto(
    int Id,
    int ProjectId,
    string ProjectKey,
    string ProjectName,
    string Name,
    ProjectRole MyRole,
    List<BoardColumnDto> Columns);

public record BoardSummaryDto(int Id, string Name);

public class SaveBoardRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
}

public class SaveColumnRequest
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = "";

    [EnumDataType(typeof(ColumnCategory))]
    public ColumnCategory Category { get; set; } = ColumnCategory.ToDo;
}

public class ReorderColumnsRequest
{
    // Every column of the board, in the order they should appear.
    [Required, MinLength(1)]
    public List<int> ColumnIds { get; set; } = [];
}
