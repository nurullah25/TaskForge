using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskForge.Api.Data;

namespace TaskForge.Api.Tests;

// Each test works inside a transaction that is never committed, so the shared
// test database stays empty for the API tests.
[Collection(ApiCollection.Name)]
public class DataModelTests(TaskForgeApiFactory factory)
{
    [Fact]
    public async Task Demo_seeder_creates_a_complete_workspace()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        await DemoDataSeeder.SeedAsync(db);

        var organization = await db.Organizations
            .Include(o => o.Members)
            .Include(o => o.Projects)
            .SingleAsync(o => o.Name == "Brightline Software");
        Assert.Equal(4, organization.Members.Count);
        Assert.Equal(2, organization.Projects.Count);

        var portal = organization.Projects.Single(p => p.Key == "CP");
        var numbers = await db.Tasks.Where(t => t.ProjectId == portal.Id).Select(t => t.Number).ToListAsync();
        Assert.Equal(Enumerable.Range(1, portal.TaskCounter), numbers.Order());
    }

    [Fact]
    public async Task Deleting_a_task_removes_its_children_but_keeps_activity_history()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await DemoDataSeeder.SeedAsync(db);

        var task = await db.Tasks.FirstAsync(t => t.Comments.Any() && t.Labels.Any());
        var taskId = task.Id;
        db.ChangeTracker.Clear();

        await db.Tasks.Where(t => t.Id == taskId).ExecuteDeleteAsync();

        Assert.False(await db.Comments.AnyAsync(c => c.TaskId == taskId));
        Assert.False(await db.TaskLabels.AnyAsync(tl => tl.TaskId == taskId));
        Assert.True(await db.ActivityLogs.AnyAsync(a => a.TaskId == null));
    }
}
