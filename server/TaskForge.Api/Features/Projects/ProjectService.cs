using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Projects;

public class ProjectService(AppDbContext db, CurrentUser currentUser, AccessService access)
{
    public async Task<List<ProjectDto>> GetForOrganizationAsync(int organizationId)
    {
        var organizationRole = await access.RequireOrganizationRoleAsync(organizationId);
        var seesAllProjects = organizationRole >= OrganizationRole.Admin;
        var userId = currentUser.Id;

        return await db.Projects
            .Where(p => p.OrganizationId == organizationId
                && (seesAllProjects || p.Members.Any(m => m.UserId == userId)))
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto(
                p.Id,
                p.OrganizationId,
                p.Key,
                p.Name,
                p.Description,
                p.Status,
                seesAllProjects ? ProjectRole.Manager : p.Members.First(m => m.UserId == userId).Role,
                p.Members.Count,
                db.Tasks.Count(t => t.ProjectId == p.Id && t.Column.Category != ColumnCategory.Done),
                p.CreatedAt))
            .ToListAsync();
    }

    public async Task<ProjectDetailsDto> GetAsync(int id)
    {
        var myRole = await access.RequireProjectRoleAsync(id);
        var userId = currentUser.Id;

        return await db.Projects
            .Where(p => p.Id == id)
            .Select(p => new ProjectDetailsDto(
                p.Id,
                p.OrganizationId,
                p.Organization.Name,
                p.Key,
                p.Name,
                p.Description,
                p.Status,
                myRole,
                p.Organization.Members.Any(m => m.UserId == userId && m.Role >= OrganizationRole.Admin),
                p.CreatedAt,
                p.Boards.OrderBy(b => b.Id).Select(b => new ProjectBoardDto(b.Id, b.Name)).ToList()))
            .SingleAsync();
    }

    public async Task<ProjectDetailsDto> CreateAsync(int organizationId, CreateProjectRequest request)
    {
        await access.RequireOrganizationRoleAsync(organizationId, OrganizationRole.Admin);

        var key = request.Key.Trim().ToUpperInvariant();
        if (await db.Projects.AnyAsync(p => p.OrganizationId == organizationId && p.Key == key))
            throw DuplicateKey(key);

        var project = new Project
        {
            OrganizationId = organizationId,
            Key = key,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Members = [new ProjectMember { UserId = currentUser.Id, Role = ProjectRole.Manager }],
            Boards = [Board.CreateWithDefaultColumns("Main board")]
        };

        db.Projects.Add(project);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            throw DuplicateKey(key);
        }

        return await GetAsync(project.Id);
    }

    public async Task<ProjectDetailsDto> UpdateAsync(int id, UpdateProjectRequest request)
    {
        await access.RequireProjectRoleAsync(id, ProjectRole.Manager);

        var project = await db.Projects.SingleAsync(p => p.Id == id);
        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.Status = request.Status;
        await db.SaveChangesAsync();

        return await GetAsync(id);
    }

    // Only organization admins can delete a project; project managers can archive it instead.
    public async Task DeleteAsync(int id)
    {
        await access.RequireProjectRoleAsync(id);
        var organizationId = await db.Projects.Where(p => p.Id == id).Select(p => p.OrganizationId).SingleAsync();
        await access.RequireOrganizationRoleAsync(organizationId, OrganizationRole.Admin);

        // Tasks and activity rows don't cascade from projects (see TaskItemConfiguration),
        // so they are deleted first. Boards, columns, labels and members cascade in the database.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.ActivityLogs.Where(a => a.ProjectId == id).ExecuteDeleteAsync();
        await db.Tasks.Where(t => t.ProjectId == id).ExecuteDeleteAsync();
        await db.Projects.Where(p => p.Id == id).ExecuteDeleteAsync();
        await transaction.CommitAsync();
    }

    public async Task<List<ProjectMemberDto>> GetMembersAsync(int id)
    {
        await access.RequireProjectRoleAsync(id);

        return await db.ProjectMembers
            .Where(m => m.ProjectId == id)
            .OrderBy(m => m.User.FullName)
            .Select(m => new ProjectMemberDto(m.UserId, m.User.FullName, m.User.Email, m.Role, m.AddedAt))
            .ToListAsync();
    }

    public async Task<ProjectMemberDto> AddMemberAsync(int id, AddProjectMemberRequest request)
    {
        await access.RequireProjectRoleAsync(id, ProjectRole.Manager);

        var user = await db.Projects
            .Where(p => p.Id == id)
            .SelectMany(p => p.Organization.Members)
            .Where(m => m.UserId == request.UserId)
            .Select(m => m.User)
            .SingleOrDefaultAsync()
            ?? throw new BadRequestException("Only members of the organization can be added to its projects.");

        if (await db.ProjectMembers.AnyAsync(m => m.ProjectId == id && m.UserId == request.UserId))
            throw new ConflictException($"{user.FullName} is already on this project.");

        var member = new ProjectMember { ProjectId = id, UserId = user.Id, Role = request.Role };
        db.ProjectMembers.Add(member);
        await db.SaveChangesAsync();

        return new ProjectMemberDto(user.Id, user.FullName, user.Email, member.Role, member.AddedAt);
    }

    public async Task<ProjectMemberDto> UpdateMemberAsync(int id, int userId, UpdateProjectMemberRequest request)
    {
        await access.RequireProjectRoleAsync(id, ProjectRole.Manager);

        var member = await db.ProjectMembers
            .Include(m => m.User)
            .SingleOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId)
            ?? throw new NotFoundException("Member not found.");

        member.Role = request.Role;
        await db.SaveChangesAsync();

        return new ProjectMemberDto(member.UserId, member.User.FullName, member.User.Email, member.Role, member.AddedAt);
    }

    public async Task RemoveMemberAsync(int id, int userId)
    {
        var isSelf = userId == currentUser.Id;
        await access.RequireProjectRoleAsync(id, isSelf ? ProjectRole.Viewer : ProjectRole.Manager);

        await using var transaction = await db.Database.BeginTransactionAsync();

        var removed = await db.ProjectMembers
            .Where(m => m.ProjectId == id && m.UserId == userId)
            .ExecuteDeleteAsync();

        if (removed == 0)
            throw new NotFoundException("Member not found.");

        await db.Tasks
            .Where(t => t.ProjectId == id && t.AssigneeId == userId && t.CompletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.AssigneeId, (int?)null));

        await transaction.CommitAsync();
    }

    private static ConflictException DuplicateKey(string key) =>
        new($"Another project in this organization already uses the key {key}.");
}
