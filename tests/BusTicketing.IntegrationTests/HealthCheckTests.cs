using System.Net;
using FluentAssertions;
using Xunit;

namespace BusTicketing.IntegrationTests;

public class HealthCheckTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(ApiWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task LivenessEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
