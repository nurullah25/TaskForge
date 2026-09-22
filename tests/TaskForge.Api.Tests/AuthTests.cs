using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskForge.Api.Features.Auth;

namespace TaskForge.Api.Tests;

[Collection(ApiCollection.Name)]
public class AuthTests(TaskForgeApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateApiClient();

    [Fact]
    public async Task Register_returns_access_token_and_sets_http_only_refresh_cookie()
    {
        var email = TaskForgeApiFactory.UniqueEmail();

        var response = await RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(auth!.AccessToken));
        Assert.Equal(email, auth.User.Email);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("tf_refresh=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_with_existing_email_returns_conflict()
    {
        var email = TaskForgeApiFactory.UniqueEmail();
        await RegisterAsync(email);

        var response = await RegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("An account with this email already exists.", problem!.Detail);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public async Task Register_with_short_password_returns_validation_errors()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "not-an-email",
            FullName = "Test User",
            Password = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Email", problem!.Errors.Keys);
        Assert.Contains("Password", problem.Errors.Keys);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_unauthorized()
    {
        var email = TaskForgeApiFactory.UniqueEmail();
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Access_token_from_login_can_call_protected_endpoint()
    {
        var email = TaskForgeApiFactory.UniqueEmail();
        await RegisterAsync(email);
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = TaskForgeApiFactory.TestPassword });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(email, user!.Email);
    }

    [Fact]
    public async Task Protected_endpoint_without_token_returns_unauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_issues_new_tokens_and_old_refresh_token_stops_working()
    {
        var registered = await RegisterAsync(TaskForgeApiFactory.UniqueEmail());
        var firstCookie = GetRefreshCookie(registered);

        var refreshed = await PostWithCookieAsync("/api/auth/refresh", firstCookie);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var secondCookie = GetRefreshCookie(refreshed);
        Assert.NotEqual(firstCookie, secondCookie);

        var reused = await PostWithCookieAsync("/api/auth/refresh", firstCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var registered = await RegisterAsync(TaskForgeApiFactory.UniqueEmail());
        var cookie = GetRefreshCookie(registered);

        var logout = await PostWithCookieAsync("/api/auth/logout", cookie);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await PostWithCookieAsync("/api/auth/refresh", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    private Task<HttpResponseMessage> RegisterAsync(string email) =>
        _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            FullName = "Test User",
            Password = TaskForgeApiFactory.TestPassword
        });

    private Task<HttpResponseMessage> PostWithCookieAsync(string url, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Cookie", cookie);
        return _client.SendAsync(request);
    }

    // Returns "tf_refresh=<value>" from the Set-Cookie header, ready to send back.
    private static string GetRefreshCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
}
