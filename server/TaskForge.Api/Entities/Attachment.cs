namespace TaskForge.Api.Entities;

public class Attachment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public int UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;

    // FileName is what the user uploaded; StoredFileName is the generated name on disk.
    public string FileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
