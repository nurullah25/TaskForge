using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;
using TaskForge.Api.Storage;

namespace TaskForge.Api.Features.Attachments;

public record AttachmentDto(
    int Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    MemberSummaryDto UploadedBy,
    DateTime CreatedAt,
    bool CanDelete);

public record AttachmentFile(Stream Content, string ContentType, string FileName);

public class AttachmentService(AppDbContext db, CurrentUser currentUser, AccessService access, IFileStorage storage)
{
    public const long MaxFileSize = 10 * 1024 * 1024;

    // Anything not on this list is rejected, so uploads can't be used to serve scripts.
    private static readonly string[] AllowedExtensions =
        [".png", ".jpg", ".jpeg", ".gif", ".webp", ".pdf", ".txt", ".csv", ".log", ".md", ".docx", ".xlsx", ".zip"];

    public async Task<List<AttachmentDto>> GetForTaskAsync(int taskId)
    {
        var (_, role) = await RequireTaskAccessAsync(taskId);
        var userId = currentUser.Id;

        return await db.Attachments
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                new MemberSummaryDto(a.UploadedBy.Id, a.UploadedBy.FullName, a.UploadedBy.Email),
                a.CreatedAt,
                a.UploadedById == userId || role == ProjectRole.Manager))
            .ToListAsync();
    }

    public async Task<AttachmentDto> UploadAsync(int taskId, IFormFile file, CancellationToken cancellationToken)
    {
        var (projectId, _) = await RequireTaskAccessAsync(taskId, ProjectRole.Contributor);

        if (file.Length == 0)
            throw new BadRequestException("The file is empty.");

        if (file.Length > MaxFileSize)
            throw new BadRequestException("Files can be up to 10 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new BadRequestException($"Files of type {extension} can't be uploaded.");

        await using var content = file.OpenReadStream();
        var storedFileName = await storage.SaveAsync(content, extension, cancellationToken);

        var attachment = new Attachment
        {
            TaskId = taskId,
            UploadedById = currentUser.Id,
            FileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length
        };

        db.Attachments.Add(attachment);
        db.ActivityLogs.Add(new ActivityLog
        {
            ProjectId = projectId,
            TaskId = taskId,
            UserId = currentUser.Id,
            Type = ActivityType.AttachmentAdded,
            NewValue = attachment.FileName
        });

        await db.SaveChangesAsync();

        var user = await db.Users.SingleAsync(u => u.Id == currentUser.Id);
        return new AttachmentDto(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            new MemberSummaryDto(user.Id, user.FullName, user.Email),
            attachment.CreatedAt,
            true);
    }

    public async Task<AttachmentFile> DownloadAsync(int attachmentId)
    {
        var attachment = await FindAsync(attachmentId);
        await RequireTaskAccessAsync(attachment.TaskId);

        return new AttachmentFile(storage.Open(attachment.StoredFileName), attachment.ContentType, attachment.FileName);
    }

    public async Task DeleteAsync(int attachmentId)
    {
        var attachment = await FindAsync(attachmentId);
        var (_, role) = await RequireTaskAccessAsync(attachment.TaskId, ProjectRole.Contributor);

        if (attachment.UploadedById != currentUser.Id && role < ProjectRole.Manager)
            throw new ForbiddenException("Only the person who uploaded the file or a project manager can delete it.");

        db.Attachments.Remove(attachment);
        await db.SaveChangesAsync();

        // The database row is what matters; a leftover file is cleaned up here afterwards.
        storage.Delete(attachment.StoredFileName);
    }

    private async Task<Attachment> FindAsync(int attachmentId) =>
        await db.Attachments.SingleOrDefaultAsync(a => a.Id == attachmentId)
        ?? throw new NotFoundException("Attachment not found.");

    private async Task<(int ProjectId, ProjectRole Role)> RequireTaskAccessAsync(
        int taskId, ProjectRole minimum = ProjectRole.Viewer)
    {
        var projectId = await db.Tasks
            .Where(t => t.Id == taskId)
            .Select(t => (int?)t.ProjectId)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Task not found.");

        return (projectId, await access.RequireProjectRoleAsync(projectId, minimum));
    }
}
