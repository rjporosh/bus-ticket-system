using BusTicketing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusTicketing.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=bus_ticketing_migration;Username=postgres;Password=postgres",
            b => b.MigrationsAssembly("BusTicketing.Infrastructure")
                  .MigrationsHistoryTable("__EFMigrationsHistory", "postgresql"));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
