using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BusTicketing.Infrastructure.Persistence.Providers;

public enum DatabaseProvider
{
    PostgreSql,
    SqlServer,
    MySql,
    Oracle
}

/// <summary>
/// The single seam through which the database engine is selected. Nothing outside this
/// class knows or cares which provider is active — swapping databases is a one-line
/// change to the "Database:Provider" configuration key, no code or migration-project
/// changes required (each provider keeps its own migrations folder, see
/// AddInfrastructure in DependencyInjection.cs).
/// </summary>
public static class DatabaseProviderExtensions
{
    private const string ConfigKey = "Database:Provider";

    public static DatabaseProvider ReadProvider(this IConfiguration configuration)
    {
        var raw = configuration[ConfigKey] ?? nameof(DatabaseProvider.PostgreSql);
        return Enum.TryParse<DatabaseProvider>(raw, ignoreCase: true, out var provider)
            ? provider
            : throw new InvalidOperationException(
                $"Unsupported \"{ConfigKey}\" value \"{raw}\". Expected one of: {string.Join(", ", Enum.GetNames<DatabaseProvider>())}.");
    }

    public static DbContextOptionsBuilder UseConfiguredProvider(
        this DbContextOptionsBuilder builder, IConfiguration configuration)
    {
        var provider = configuration.ReadProvider();
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing \"ConnectionStrings:Default\" configuration value.");

        return provider switch
        {
            DatabaseProvider.PostgreSql => builder.UseNpgsql(
                connectionString,
                b => b.MigrationsAssembly("BusTicketing.Infrastructure")
                      .MigrationsHistoryTable("__EFMigrationsHistory", "postgresql")),

            DatabaseProvider.SqlServer => builder.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly("BusTicketing.Infrastructure")
                      .MigrationsHistoryTable("__EFMigrationsHistory", "sqlserver")),

            DatabaseProvider.MySql => builder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                b => b.MigrationsAssembly("BusTicketing.Infrastructure")
                      .MigrationsHistoryTable("__EFMigrationsHistory", "mysql")),

            DatabaseProvider.Oracle => builder.UseOracle(
                connectionString,
                b => b.MigrationsAssembly("BusTicketing.Infrastructure")
                      .MigrationsHistoryTable("__EFMigrationsHistory", "ORACLE")),

            _ => throw new InvalidOperationException($"Unsupported database provider: {provider}")
        };
    }
}
