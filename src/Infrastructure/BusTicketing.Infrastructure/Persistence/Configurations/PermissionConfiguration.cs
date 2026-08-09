using BusTicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTicketing.Infrastructure.Persistence.Configurations;

public class TicketNumberCounterConfiguration : IEntityTypeConfiguration<TicketNumberCounter>
{
    public void Configure(EntityTypeBuilder<TicketNumberCounter> builder)
    {
        builder.ToTable("TicketNumberCounters");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(c => c.CounterDate).HasColumnType("date").IsRequired();
        builder.Property(c => c.LastNumber).IsRequired();

        builder.HasIndex(c => c.CounterDate).IsUnique();

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(rp => rp.Permission).HasConversion<int>().IsRequired();

        builder.HasIndex(rp => new { rp.RoleId, rp.Permission }).IsUnique();

        builder.HasOne(rp => rp.Role)
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(rp => !rp.IsDeleted);
    }
}
