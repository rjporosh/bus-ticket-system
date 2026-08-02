using BusTicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTicketing.Infrastructure.Persistence.Configurations;

public class BusConfiguration : IEntityTypeConfiguration<Bus>
{
    public void Configure(EntityTypeBuilder<Bus> builder)
    {
        builder.ToTable("Buses");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(b => b.Number).HasMaxLength(30).IsRequired();
        builder.Property(b => b.RegistrationNumber).HasMaxLength(30).IsRequired();
        builder.Property(b => b.OperatorName).HasMaxLength(150).IsRequired();

        builder.HasIndex(b => b.RegistrationNumber).IsUnique();
        builder.HasIndex(b => b.Number).IsUnique();

        builder.HasOne(b => b.SeatLayout)
            .WithOne()
            .HasForeignKey<SeatLayout>(l => l.BusId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

public class SeatLayoutConfiguration : IEntityTypeConfiguration<SeatLayout>
{
    public void Configure(EntityTypeBuilder<SeatLayout> builder)
    {
        builder.ToTable("SeatLayouts");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasIndex(l => l.BusId).IsUnique();

        builder.HasMany(l => l.Seats)
            .WithOne()
            .HasForeignKey(s => s.SeatLayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(s => s.SeatNumber).HasMaxLength(10).IsRequired();
        builder.Property(s => s.RowLabel).HasMaxLength(2).IsRequired();

        builder.HasIndex(s => new { s.SeatLayoutId, s.SeatNumber }).IsUnique();

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
