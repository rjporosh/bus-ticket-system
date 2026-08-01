using BusTicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTicketing.Infrastructure.Persistence.Configurations;

public class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("Stations");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.City).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Address).HasMaxLength(300);

        builder.HasIndex(s => new { s.Name, s.City }).IsUnique();

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.DistanceKm).HasColumnType("decimal(9,2)");

        builder.HasOne(r => r.Origin)
            .WithMany()
            .HasForeignKey(r => r.OriginStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Destination)
            .WithMany()
            .HasForeignKey(r => r.DestinationStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.OriginStationId, r.DestinationStationId }).IsUnique();

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
