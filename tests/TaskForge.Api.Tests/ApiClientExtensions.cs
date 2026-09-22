using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskForge.Api.Features.Organizations;
using TaskForge.Api.Features.Projects;

namespace TaskForge.Api.Tests;

// Small helpers so tests describe scenarios instead of HTTP plumbing.
public static class ApiClientExtensions
{
    // Matches the API: camelCase properties and enums as strings.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    public static async Task<T> GetJsonAsync<T>(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync<T>();
    }

    public static async Task<OrganizationDto> CreateOrganizationAsync(this HttpClient client, string name = "Acme")
    {
        var response = await client.PostAsJsonAsync("/api/organizations", new { Name = name });
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync<OrganizationDto>();
    }

    public static async Task AddOrganizationMemberAsync(this HttpClient client, int organizationId, string email, string role = "Member")
    {
        var response = await client.PostAsJsonAsync($"/api/organizations/{organizationId}/members", new { Email = email, Role = role });
        response.EnsureSuccessStatusCode();
    }

    public static async Task<ProjectDetailsDto> CreateProjectAsync(this HttpClient client, int organizationId, string key = "WEB")
    {
        var response = await client.PostAsJsonAsync($"/api/organizations/{organizationId}/projects",
            new { Name = $"Project {key}", Key = key });
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync<ProjectDetailsDto>();
    }

    public static async Task AddProjectMemberAsync(this HttpClient client, int projectId, int userId, string role)
    {
        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/members", new { UserId = userId, Role = role });
        response.EnsureSuccessStatusCode();
    }
}
