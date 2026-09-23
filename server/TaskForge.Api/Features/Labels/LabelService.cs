using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Labels;

public class LabelService(AppDbContext db, AccessService access)
{
    public async Task<List<LabelDto>> GetForProjectAsync(int projectId)
    {
        await access.RequireProjectRoleAsync(projectId);

        return await db.Labels
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelDto(l.Id, l.Name, l.Color))
            .ToListAsync();
    }

    public async Task<LabelDto> CreateAsync(int projectId, SaveLabelRequest request)
    {
        await access.RequireProjectRoleAsync(projectId, ProjectRole.Manager);

        var label = new Label
        {
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Color = request.Color.ToLowerInvariant()
        };

        db.Labels.Add(label);
        await SaveAsync(label.Name);

        return new LabelDto(label.Id, label.Name, label.Color);
    }

    public async Task<LabelDto> UpdateAsync(int labelId, SaveLabelRequest request)
    {
        var label = await FindAsync(labelId);
        await access.RequireProjectRoleAsync(label.ProjectId, ProjectRole.Manager);

        label.Name = request.Name.Trim();
        label.Color = request.Color.ToLowerInvariant();
        await SaveAsync(label.Name);

        return new LabelDto(label.Id, label.Name, label.Color);
    }

    // Deleting a label takes it off every task that used it (the link rows cascade).
    public async Task DeleteAsync(int labelId)
    {
        var label = await FindAsync(labelId);
        await access.RequireProjectRoleAsync(label.ProjectId, ProjectRole.Manager);

        db.Labels.Remove(label);
        await db.SaveChangesAsync();
    }

    public async Task<List<LabelDto>> SetTaskLabelsAsync(int taskId, SetTaskLabelsRequest request)
    {
        var task = await db.Tasks
            .Include(t => t.Labels)
            .SingleOrDefaultAsync(t => t.Id == taskId)
            ?? throw new NotFoundException("Task not found.");

        await access.RequireProjectRoleAsync(task.ProjectId, ProjectRole.Contributor);

        var wanted = request.LabelIds.Distinct().ToList();
        var labels = await db.Labels
            .Where(l => l.ProjectId == task.ProjectId && wanted.Contains(l.Id))
            .ToListAsync();

        if (labels.Count != wanted.Count)
            throw new BadRequestException("Tasks can only use labels from their own project.");

        task.Labels.RemoveAll(tl => !wanted.Contains(tl.LabelId));
        foreach (var label in labels.Where(l => task.Labels.All(tl => tl.LabelId != l.Id)))
        {
            task.Labels.Add(new TaskLabel { LabelId = label.Id });
        }

        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return labels.OrderBy(l => l.Name).Select(l => new LabelDto(l.Id, l.Name, l.Color)).ToList();
    }

    private async Task<Label> FindAsync(int labelId) =>
        await db.Labels.SingleOrDefaultAsync(l => l.Id == labelId)
        ?? throw new NotFoundException("Label not found.");

    private async Task SaveAsync(string name)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            throw new ConflictException($"This project already has a label called {name}.");
        }
    }
}
