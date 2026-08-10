using FluentAssertions;
using Xunit;

namespace BusTicketing.IntegrationTests;

/// <summary>
/// Smoke tests for the printable ticket route registration and behavior.
/// Full authentication-based integration tests require a migrated PostgreSQL database
/// with the latest schema (Age column, RealBus columns) and seeded permissions.
/// </summary>
public class TicketPrintRouteTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketPrintRouteTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PrintEndpoint_Returns401_WhenNotAuthenticated_EvenWithInvalidTicket()
    {
        var response = await _client.GetAsync("/api/v1/booking/tickets/00000000-0000-0000-0000-000000000000/print");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}
