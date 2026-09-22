using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Common;

// Central place for "is the current user allowed to do this?" checks.
// Roles live in the database (not in the JWT) so a role change applies immediately.
public class AccessService(AppDbContext db, CurrentUser currentUser)
{
    public async Task<OrganizationRole> RequireOrganizationRoleAsync(
        int organizationId, OrganizationRole minimum = OrganizationRole.Member)
    {
        var userId = currentUser.Id;
        var role = await db.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId)
            .Select(m => (OrganizationRole?)m.Role)
            .SingleOrDefaultAsync();

        // Outsiders get 404 instead of 403, so they can't find out which ids exist.
        if (role == null)
            throw new NotFoundException("Organization not found.");

        if (role < minimum)
            throw new ForbiddenException();

        return role.Value;
    }

    public async Task<ProjectRole> RequireProjectRoleAsync(int projectId, ProjectRole minimum = ProjectRole.Viewer)
    {
        var userId = currentUser.Id;
        var access = await db.Projects
            .Where(p => p.Id == projectId)
            .Select(p => new
            {
                OrganizationRole = p.Organization.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (OrganizationRole?)m.Role)
                    .SingleOrDefault(),
                ProjectRole = p.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (ProjectRole?)m.Role)
                    .SingleOrDefault()
            })
            .SingleOrDefaultAsync();

        var role = GetEffectiveProjectRole(access?.OrganizationRole, access?.ProjectRole)
            ?? throw new NotFoundException("Project not found.");

        if (role < minimum)
            throw new ForbiddenException();

        return role;
    }

    // Organization admins and owners manage every project in their organization,
    // everyone else needs to be a member of the project.
    public static ProjectRole? GetEffectiveProjectRole(OrganizationRole? organizationRole, ProjectRole? projectRole) =>
        organizationRole switch
        {
            null => null,
            >= OrganizationRole.Admin => ProjectRole.Manager,
            _ => projectRole
        };
}
