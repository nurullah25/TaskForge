using System.Net;

namespace TaskForge.Api.Tests;

[Collection(ApiCollection.Name)]
public class HealthCheckTests(TaskForgeApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_reports_healthy_when_database_is_reachable()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
