namespace TaskForge.Api.Entities;

public class ActivityLog
{
    public long Id { get; set; }
    public int ProjectId { get; set; }

    // Null once the task is deleted, so project history survives task deletion.
    public int? TaskId { get; set; }
    public TaskItem? Task { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ActivityType Type { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
