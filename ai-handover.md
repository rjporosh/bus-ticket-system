# AI Handover — BusTicketingSystem Integration Tests

## Task
Get the integration test suite (`BusTicketing.IntegrationTests`) passing.

## Completed
- Created `tests/BusTicketing.IntegrationTests/ApiWebApplicationFactory.cs` with:
  - `UseEnvironment("Testing")`
  - In-memory config overrides: `SeedData:Enabled=false`, `Database:Testing=true`, `Database:TestingDatabaseName=bus-ticketing-tests-{Guid}`, test JWT secret/issuer/audience
- Verified `SmsNotificationTests.cs` constructor only initializes `_factory` and `_client` (no manual `ApplicationDbContext` scope).

## Current Failure
Running `dotnet test tests/BusTicketing.IntegrationTests/BusTicketing.IntegrationTests.csproj` produces:

```
System.InvalidOperationException: The entry point exited without ever building an IHost.
   at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`1.CreateClient()
   ...
```

**Results:** `Failed: 4, Passed: 10, Skipped: 0, Total: 14`

The failing tests are in `SmsNotificationTests` (and potentially any test class that calls `factory.CreateClient()` in the constructor). Tests that don't trigger host building currently pass.

## Root Cause
`src/Presentation/BusTicketing.Api/Program.cs` uses top-level statements that call `app.Run()` (line 146). This is incompatible with `WebApplicationFactory<Program>` because:

1. `WebApplicationFactory` cannot properly intercept the host when `app.Run()` is invoked inside top-level statements.
2. The `try/catch` in `Program.cs` (lines 26–155) swallows any startup exception, logs it, and exits — so the factory sees the entry point exit without a built host.

Additionally, `DependencyInjection.cs` reads `JwtSettings` at registration time (line 62–63) and throws `InvalidOperationException` if the section is missing. Because top-level `Program.cs` runs before `ApiWebApplicationFactory.ConfigureAppConfiguration` is applied, the JWT config may not be available when `AddInfrastructure(builder.Configuration)` is called.

## Next Steps
1. **Refactor `Program.cs`** to make it testable:
   - Move all top-level statements into an explicit `public partial class Program` with a `public static async Task Main(string[] args)` method.
   - Ensure `app.Run()` is only called when NOT in the `Testing` environment (or restructure so `WebApplicationFactory` can build the host without running it).
2. **Make JWT registration resilient** in `DependencyInjection.cs` so it does not throw if the `Jwt` section is missing during early registration, or defer validation.
3. Run `dotnet test tests/BusTicketing.IntegrationTests/BusTicketing.IntegrationTests.csproj` and confirm all 14 tests pass.

## Relevant Files
- `tests/BusTicketing.IntegrationTests/ApiWebApplicationFactory.cs`
- `tests/BusTicketing.IntegrationTests/SmsNotificationTests.cs`
- `src/Presentation/BusTicketing.Api/Program.cs`
- `src/Infrastructure/BusTicketing.Infrastructure/DependencyInjection.cs`
