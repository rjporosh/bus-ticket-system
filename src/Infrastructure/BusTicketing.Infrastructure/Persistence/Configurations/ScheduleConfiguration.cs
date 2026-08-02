using BusTicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTicketing.Infrastructure.Persistence.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("Schedules");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(s => s.FareAmount).HasColumnType("decimal(9,2)");
        builder.Property(s => s.DaysOfWeek).HasConversion<int>();
        builder.Property(s => s.Status).HasConversion<int>();

        builder.HasOne(s => s.Bus)
            .WithMany()
            .HasForeignKey(s => s.BusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Route)
            .WithMany()
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.BusId, s.DepartureTime });
        builder.HasIndex(s => s.RouteId);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
