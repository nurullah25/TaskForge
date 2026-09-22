using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskForge.Api.Data;

namespace TaskForge.Api.Tests;

// Runs the real API in memory against a dedicated SQL Server database.
// SQL Server is used instead of the EF in-memory provider so that constraints,
// cascade rules and rowversion behave exactly like they do in the real app.
public class TaskForgeApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=TaskForge_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("TASKFORGE_TEST_DB") ?? DefaultConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "false");
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<TaskForgeApiFactory>
{
    public const string Name = "Api";
}
