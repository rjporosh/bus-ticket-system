using System.Net;
using System.Net.Http.Json;
using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Domain.Entities;
using BusTicketing.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BusTicketing.IntegrationTests;

public class AuthControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Username, string Password)> SeedTestUserAsync()
    {
        const string username = "test_booth_staff";
        const string password = "TestPass@123";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (!db.Roles.Any(r => r.Name == "BoothStaff"))
            db.Roles.Add(Role.Create("BoothStaff", isSystemRole: true));
        await db.SaveChangesAsync();

        var role = db.Roles.First(r => r.Name == "BoothStaff");

        if (!db.Users.Any(u => u.Username == username))
        {
            var user = User.Create(username, "test.staff@example.com", hasher.Hash(password), "Test Staff", role.Id);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        return (username, password);
    }

    [Fact]
    public async Task Login_WithEmptyBody_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "", Password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "does_not_exist", Password = "whatever123" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        var (username, password) = await SeedTestUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { Username = username, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseShape>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.User.Username.Should().Be(username);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var (username, _) = await SeedTestUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { Username = username, Password = "totally-wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ImmediatelyReusingARotatedToken_RevokesTheSession()
    {
        var (username, password) = await SeedTestUserAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { Username = username, Password = password });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseShape>();

        // First use rotates the token and succeeds.
        var firstRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = loginBody!.RefreshToken });
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reusing the same (now-revoked) token must fail.
        var secondRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = loginBody.RefreshToken });
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record LoginResponseShape(string AccessToken, string RefreshToken, LoginUserShape User);
    private record LoginUserShape(string Username);
}
