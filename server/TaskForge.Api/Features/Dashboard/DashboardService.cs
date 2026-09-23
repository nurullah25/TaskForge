using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;
using TaskForge.Api.Features.Tasks;

namespace TaskForge.Api.Features.Dashboard;

public record CountByName(string Name, int Count);

public record DashboardDto(
    int ProjectCount,
    int OpenTaskCount,
    int CompletedTaskCount,
    int OverdueTaskCount,
    int MyOpenTaskCount,
    List<CountByName> OpenByCategory,
    List<CountByName> OpenByPriority,
    List<TaskListItemDto> MyTasks);

public class DashboardService(AppDbContext db, CurrentUser currentUser, AccessService access)
{
    public async Task<DashboardDto> GetAsync(int organizationId)
    {
        var organizationRole = await access.RequireOrganizationRoleAsync(organizationId);
        var userId = currentUser.Id;
        var seesAllProjects = organizationRole >= OrganizationRole.Admin;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = db.Projects.Where(p => p.OrganizationId == organizationId
            && (seesAllProjects || p.Members.Any(m => m.UserId == userId)));

        // Every count below is one query over the tasks of the projects this user can see.
        var tasks = db.Tasks.Where(t => projects.Any(p => p.Id == t.ProjectId));
        var openTasks = tasks.Where(t => t.Column.Category != ColumnCategory.Done);

        var openByCategory = await openTasks
            .GroupBy(t => t.Column.Category)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var openByPriority = await openTasks
            .GroupBy(t => t.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var myTasks = await openTasks
            .Where(t => t.AssigneeId == userId)
            .OrderBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .Take(8)
            .Select(TaskSearchService.ListItem)
            .ToListAsync();

        return new DashboardDto(
            await projects.CountAsync(),
            await openTasks.CountAsync(),
            await tasks.CountAsync(t => t.CompletedAt != null),
            await openTasks.CountAsync(t => t.DueDate < today),
            await openTasks.CountAsync(t => t.AssigneeId == userId),
            openByCategory.Select(x => new CountByName(x.Key.ToString(), x.Count)).ToList(),
            openByPriority.OrderByDescending(x => x.Key).Select(x => new CountByName(x.Key.ToString(), x.Count)).ToList(),
            myTasks);
    }
}
