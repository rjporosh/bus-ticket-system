using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BusTicketing.IntegrationTests;

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
    }
}
