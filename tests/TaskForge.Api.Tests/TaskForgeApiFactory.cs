using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskForge.Api.Data;
using TaskForge.Api.Features.Auth;

namespace TaskForge.Api.Tests;

// Runs the real API in memory against a dedicated SQL Server database.
// SQL Server is used instead of the EF in-memory provider so that constraints,
// cascade rules and rowversion behave exactly like they do in the real app.
public class TaskForgeApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestPassword = "Password123!";

    private const string DefaultConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=TaskForge_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("TASKFORGE_TEST_DB") ?? DefaultConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789");
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    // HTTPS base address because the refresh cookie is marked Secure. Cookies are handled
    // manually in tests so each test can see exactly which cookie it sends.
    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false
    });

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    // Registers a brand new user and returns a client that sends their access token.
    public async Task<(HttpClient Client, UserDto User)> CreateSignedInClientAsync(string fullName = "Test User")
    {
        var (client, user, _) = await CreateSignedInUserAsync(fullName);
        return (client, user);
    }

    // Same, but also hands back the raw token for tests that open a hub connection.
    public async Task<(HttpClient Client, UserDto User, string AccessToken)> CreateSignedInUserAsync(
        string fullName = "Test User")
    {
        var client = CreateApiClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = UniqueEmail(),
            FullName = fullName,
            Password = TestPassword
        });
        response.EnsureSuccessStatusCode();

        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth.User, auth.AccessToken);
    }

    // A real SignalR connection that talks to the in-memory test server.
    public HubConnection CreateHubConnection(string accessToken) =>
        new HubConnectionBuilder()
            .WithUrl($"{Server.BaseAddress}hubs/app", options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            // The hub sends enums as strings, like the REST API, so the client is told to expect that.
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<TaskForgeApiFactory>
{
    public const string Name = "Api";
}
