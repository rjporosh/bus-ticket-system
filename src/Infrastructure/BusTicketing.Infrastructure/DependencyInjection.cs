using System.Text;
using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using BusTicketing.Infrastructure.Persistence;
using BusTicketing.Infrastructure.Persistence.Providers;
using BusTicketing.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BusTicketing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddSingleton<QueryLoggingInterceptor>();

        // The integration tests (WebApplicationFactory<Program>) set "Testing" in
        // MemoryDatabase and config to disable the real provider. This avoids the
        // Npgsql/InMemory provider conflict by short-circuiting before UseConfiguredProvider.
        var inTesting = configuration.GetValue<string>("Database:Testing") == "true";
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            if (inTesting)
            {
                var testingDatabaseName = configuration.GetValue("Database:TestingDatabaseName", "BusTicketingTesting");
                options.UseInMemoryDatabase(testingDatabaseName);
            }
            else
            {
                options.UseConfiguredProvider(configuration);
            }
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<QueryLoggingInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Missing \"Jwt\" configuration section.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole(Domain.Enums.SystemRoles.Admin))
            .AddPolicy("BoothStaffOrAdmin", policy => policy.RequireRole(Domain.Enums.SystemRoles.Admin, Domain.Enums.SystemRoles.BoothStaff));

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready" });

        return services;
    }
}
