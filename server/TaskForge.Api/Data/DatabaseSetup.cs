using Microsoft.EntityFrameworkCore;

namespace TaskForge.Api.Data;

public static class DatabaseSetup
{
    // Development convenience: apply pending migrations and add demo data on startup.
    // In production, migrations would run as a deployment step instead.
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        var settings = app.Configuration.GetSection("Database");
        if (!settings.GetValue<bool>("MigrateOnStartup"))
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        if (settings.GetValue<bool>("SeedDemoData"))
            await DemoDataSeeder.SeedAsync(db);
    }
}
