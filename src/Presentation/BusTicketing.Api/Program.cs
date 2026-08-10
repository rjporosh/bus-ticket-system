using Asp.Versioning;
using BusTicketing.Api.Authorization;
using BusTicketing.Api.Middleware;
using BusTicketing.Application;
using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Domain.Enums;
using BusTicketing.Infrastructure;
using BusTicketing.Infrastructure.Persistence;
using BusTicketing.Infrastructure.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .CreateLogger();

ICustomLogger? customLogger = null;

try
{
    var builder = WebApplication.CreateBuilder(args);

    var logsRootPath = Path.Combine(builder.Environment.ContentRootPath, "logs");
    customLogger = new CustomLogger(logsRootPath);
    builder.Services.AddSingleton<ICustomLogger>(customLogger);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(logsRootPath, "log-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi("v1");

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowConfiguredOrigins", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
            else
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
        });
    });

    foreach (Permission permission in Enum.GetValues(typeof(Permission)))
    {
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy($"Permission:{permission}", policy => policy.Requirements.Add(new PermissionRequirement(permission)));
        });
    }

    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<RateLimitMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseRuntimeErrorLogger();
    app.UseGracefulShutdown();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "Bus Ticketing System API";
            options.Theme = ScalarTheme.BluePlanet;
        });
    }
    else
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowConfiguredOrigins");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    if (builder.Configuration.GetValue("SeedData:Enabled", true))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        DataSeeder.SeedAsync(db, hasher, seedLogger).GetAwaiter().GetResult();
    }

    Log.Information("Bus Ticketing System API starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    if (customLogger != null)
    {
        await customLogger.LogRuntimeErrorAsync("Application terminated unexpectedly during startup", ex);
    }
}
finally
{
    Log.CloseAndFlush();
}

if (customLogger != null)
{
    AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Error(ex, "Unhandled exception caught by AppDomain");
            customLogger.LogRuntimeErrorAsync("Unhandled AppDomain exception", ex).GetAwaiter().GetResult();
        }
    };

    TaskScheduler.UnobservedTaskException += (sender, e) =>
    {
        Log.Error(e.Exception, "Unobserved task exception");
        customLogger.LogRuntimeErrorAsync("Unobserved task exception", e.Exception).GetAwaiter().GetResult();
    };
}

/// <summary>Partial Program class so WebApplicationFactory<Program> can be used from integration tests.</summary>
public partial class Program { }
