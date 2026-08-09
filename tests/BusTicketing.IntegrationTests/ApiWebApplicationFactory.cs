using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BusTicketing.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (middleware, auth, MediatR, validators) against an
/// isolated in-memory database per factory instance, so tests exercise genuine
/// request/response behavior without needing a running PostgreSQL container.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"bus-ticketing-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedData:Enabled"] = "false",
                ["Database:Testing"] = "true",
                ["Database:TestingDatabaseName"] = _databaseName,
                ["Jwt:Secret"] = "integration-test-secret-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "BusTicketingSystem.Tests",
                ["Jwt:Audience"] = "BusTicketingSystem.Tests.Clients"
            });
        });

        // The Infrastructure DI handles the InMemory provider selection based on
        // Database:Testing=true and uses Database:TestingDatabaseName to keep each
        // factory's database isolated (xUnit runs test classes in parallel, so a
        // shared hardcoded name caused cross-factory data races).
    }
}
