using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Organizations;

public class OrganizationService(AppDbContext db, CurrentUser currentUser, AccessService access)
{
    public async Task<List<OrganizationDto>> GetMineAsync()
    {
        var userId = currentUser.Id;

        return await db.OrganizationMembers
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Organization.Name)
            .Select(m => new OrganizationDto(
                m.OrganizationId,
                m.Organization.Name,
                m.Role,
                m.Organization.Members.Count,
                m.Organization.Projects.Count))
            .ToListAsync();
    }

    public async Task<OrganizationDto> GetAsync(int id)
    {
        var myRole = await access.RequireOrganizationRoleAsync(id);

        return await db.Organizations
            .Where(o => o.Id == id)
            .Select(o => new OrganizationDto(o.Id, o.Name, myRole, o.Members.Count, o.Projects.Count))
            .SingleAsync();
    }

    public async Task<OrganizationDto> CreateAsync(SaveOrganizationRequest request)
    {
        var organization = new Organization
        {
            Name = request.Name.Trim(),
            Members = [new OrganizationMember { UserId = currentUser.Id, Role = OrganizationRole.Owner }]
        };

        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        return new OrganizationDto(organization.Id, organization.Name, OrganizationRole.Owner, 1, 0);
    }

    public async Task<OrganizationDto> UpdateAsync(int id, SaveOrganizationRequest request)
    {
        await access.RequireOrganizationRoleAsync(id, OrganizationRole.Admin);

        var organization = await db.Organizations.SingleAsync(o => o.Id == id);
        organization.Name = request.Name.Trim();
        await db.SaveChangesAsync();

        return await GetAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        await access.RequireOrganizationRoleAsync(id, OrganizationRole.Owner);

        if (await db.Projects.AnyAsync(p => p.OrganizationId == id))
            throw new ConflictException("Delete the organization's projects before deleting the organization.");

        await db.Organizations.Where(o => o.Id == id).ExecuteDeleteAsync();
    }

    public async Task<List<OrganizationMemberDto>> GetMembersAsync(int id)
    {
        await access.RequireOrganizationRoleAsync(id);

        return await db.OrganizationMembers
            .Where(m => m.OrganizationId == id)
            .OrderBy(m => m.User.FullName)
            .Select(m => new OrganizationMemberDto(m.UserId, m.User.FullName, m.User.Email, m.Role, m.JoinedAt))
            .ToListAsync();
    }

    public async Task<OrganizationMemberDto> AddMemberAsync(int id, AddOrganizationMemberRequest request)
    {
        var myRole = await access.RequireOrganizationRoleAsync(id, OrganizationRole.Admin);

        if (request.Role == OrganizationRole.Owner && myRole != OrganizationRole.Owner)
            throw new ForbiddenException("Only owners can add other owners.");

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email)
            ?? throw new BadRequestException("No TaskForge account uses this email. Ask them to sign up first.");

        if (await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == id && m.UserId == user.Id))
            throw new ConflictException($"{user.FullName} is already a member.");

        var member = new OrganizationMember { OrganizationId = id, UserId = user.Id, Role = request.Role };
        db.OrganizationMembers.Add(member);
        await db.SaveChangesAsync();

        return new OrganizationMemberDto(user.Id, user.FullName, user.Email, member.Role, member.JoinedAt);
    }

    public async Task<OrganizationMemberDto> UpdateMemberAsync(int id, int userId, UpdateOrganizationMemberRequest request)
    {
        var myRole = await access.RequireOrganizationRoleAsync(id, OrganizationRole.Admin);
        var member = await FindMemberAsync(id, userId);

        var touchesOwner = member.Role == OrganizationRole.Owner || request.Role == OrganizationRole.Owner;
        if (touchesOwner && myRole != OrganizationRole.Owner)
            throw new ForbiddenException("Only owners can change who is an owner.");

        if (member.Role == OrganizationRole.Owner && request.Role != OrganizationRole.Owner)
            await EnsureAnotherOwnerExistsAsync(id, userId);

        member.Role = request.Role;
        await db.SaveChangesAsync();

        return new OrganizationMemberDto(member.UserId, member.User.FullName, member.User.Email, member.Role, member.JoinedAt);
    }

    // Admins remove other people; any member can remove themselves (leave).
    public async Task RemoveMemberAsync(int id, int userId)
    {
        var isSelf = userId == currentUser.Id;
        var myRole = await access.RequireOrganizationRoleAsync(id, isSelf ? OrganizationRole.Member : OrganizationRole.Admin);
        var member = await FindMemberAsync(id, userId);

        if (member.Role == OrganizationRole.Owner)
        {
            if (!isSelf && myRole != OrganizationRole.Owner)
                throw new ForbiddenException("Only owners can remove an owner.");

            await EnsureAnotherOwnerExistsAsync(id, userId);
        }

        // Leaving an organization also means leaving its projects. Open tasks are unassigned
        // so they don't stay with someone who can no longer see them.
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.ProjectMembers
            .Where(pm => pm.UserId == userId && pm.Project.OrganizationId == id)
            .ExecuteDeleteAsync();

        await db.Tasks
            .Where(t => t.AssigneeId == userId && t.Project.OrganizationId == id && t.CompletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.AssigneeId, (int?)null));

        db.OrganizationMembers.Remove(member);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task<OrganizationMember> FindMemberAsync(int organizationId, int userId) =>
        await db.OrganizationMembers
            .Include(m => m.User)
            .SingleOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId)
        ?? throw new NotFoundException("Member not found.");

    private async Task EnsureAnotherOwnerExistsAsync(int organizationId, int userId)
    {
        var hasOtherOwner = await db.OrganizationMembers.AnyAsync(m =>
            m.OrganizationId == organizationId && m.Role == OrganizationRole.Owner && m.UserId != userId);

        if (!hasOtherOwner)
            throw new ConflictException("An organization needs at least one owner. Make someone else an owner first.");
    }
}
