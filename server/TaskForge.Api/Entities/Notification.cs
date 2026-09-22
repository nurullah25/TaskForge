namespace TaskForge.Api.Entities;

public class Notification
{
    public long Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Message { get; set; } = "";

    public int? TaskId { get; set; }
    public TaskItem? Task { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
