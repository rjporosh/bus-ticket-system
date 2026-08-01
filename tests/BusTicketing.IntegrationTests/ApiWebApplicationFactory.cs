using BusTicketing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                ["Jwt:Secret"] = "integration-test-secret-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "BusTicketingSystem.Tests",
                ["Jwt:Audience"] = "BusTicketingSystem.Tests.Clients"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
