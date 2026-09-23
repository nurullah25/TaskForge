using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Comments;

public class CommentService(AppDbContext db, CurrentUser currentUser, AccessService access)
{
    public async Task<PagedResult<CommentDto>> GetForTaskAsync(int taskId, PageQuery page)
    {
        var role = await RequireTaskAccessAsync(taskId);
        var userId = currentUser.Id;

        var query = db.Comments.Where(c => c.TaskId == taskId);
        var total = await query.CountAsync();

        var comments = await query
            .OrderBy(c => c.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(c => new CommentDto(
                c.Id,
                new MemberSummaryDto(c.Author.Id, c.Author.FullName, c.Author.Email),
                c.Body,
                c.CreatedAt,
                c.UpdatedAt,
                c.AuthorId == userId,
                c.AuthorId == userId || role == ProjectRole.Manager))
            .ToListAsync();

        return new PagedResult<CommentDto>(comments, page.Page, page.PageSize, total);
    }

    // Viewers may comment too: reading and discussing is allowed, changing tasks is not.
    public async Task<CommentDto> AddAsync(int taskId, SaveCommentRequest request)
    {
        var task = await db.Tasks.SingleOrDefaultAsync(t => t.Id == taskId)
            ?? throw new NotFoundException("Task not found.");
        await access.RequireProjectRoleAsync(task.ProjectId);

        var comment = new Comment
        {
            TaskId = taskId,
            AuthorId = currentUser.Id,
            Body = request.Body.Trim()
        };

        db.Comments.Add(comment);
        db.ActivityLogs.Add(new ActivityLog
        {
            ProjectId = task.ProjectId,
            TaskId = taskId,
            UserId = currentUser.Id,
            Type = ActivityType.CommentAdded
        });

        await db.SaveChangesAsync();

        return await ReadAsync(comment.Id);
    }

    public async Task<CommentDto> UpdateAsync(int commentId, SaveCommentRequest request)
    {
        var comment = await FindAsync(commentId);
        await RequireTaskAccessAsync(comment.TaskId);

        if (comment.AuthorId != currentUser.Id)
            throw new ForbiddenException("Only the author can edit a comment.");

        comment.Body = request.Body.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await ReadAsync(comment.Id);
    }

    public async Task DeleteAsync(int commentId)
    {
        var comment = await FindAsync(commentId);
        var role = await RequireTaskAccessAsync(comment.TaskId);

        if (comment.AuthorId != currentUser.Id && role < ProjectRole.Manager)
            throw new ForbiddenException("Only the author or a project manager can delete a comment.");

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
    }

    private async Task<Comment> FindAsync(int commentId) =>
        await db.Comments.SingleOrDefaultAsync(c => c.Id == commentId)
        ?? throw new NotFoundException("Comment not found.");

    private async Task<ProjectRole> RequireTaskAccessAsync(int taskId)
    {
        var projectId = await db.Tasks
            .Where(t => t.Id == taskId)
            .Select(t => (int?)t.ProjectId)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Task not found.");

        return await access.RequireProjectRoleAsync(projectId);
    }

    private async Task<CommentDto> ReadAsync(int commentId)
    {
        var userId = currentUser.Id;

        return await db.Comments
            .Where(c => c.Id == commentId)
            .Select(c => new CommentDto(
                c.Id,
                new MemberSummaryDto(c.Author.Id, c.Author.FullName, c.Author.Email),
                c.Body,
                c.CreatedAt,
                c.UpdatedAt,
                c.AuthorId == userId,
                true))
            .SingleAsync();
    }
}
