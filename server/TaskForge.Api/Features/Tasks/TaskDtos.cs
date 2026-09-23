using System.ComponentModel.DataAnnotations;
using TaskForge.Api.Common;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Tasks;

// What a card on the board shows.
public record TaskCardDto(
    int Id,
    int Number,
    string Title,
    TaskPriority Priority,
    DateOnly? DueDate,
    int ColumnId,
    double Position,
    MemberSummaryDto? Assignee,
    bool HasDescription,
    int CommentCount);

public record TaskDetailsDto(
    int Id,
    int Number,
    int ProjectId,
    string ProjectKey,
    int BoardId,
    int ColumnId,
    string ColumnName,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate,
    MemberSummaryDto? Assignee,
    MemberSummaryDto Reporter,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    ProjectRole MyRole,
    string RowVersion);

public class CreateTaskRequest
{
    public int ColumnId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(10000)]
    public string? Description { get; set; }

    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public int? AssigneeId { get; set; }
    public DateOnly? DueDate { get; set; }
}

public class UpdateTaskRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(10000)]
    public string? Description { get; set; }

    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority Priority { get; set; }

    public int? AssigneeId { get; set; }
    public DateOnly? DueDate { get; set; }

    // The value the client last read. An empty value skips the check.
    public string? RowVersion { get; set; }
}

// The client sends the neighbours instead of an index: "put it between these two tasks".
// That stays correct even if someone else changed the column in the meantime.
public class MoveTaskRequest
{
    public int ColumnId { get; set; }
    public int? AboveTaskId { get; set; }
    public int? BelowTaskId { get; set; }
}
