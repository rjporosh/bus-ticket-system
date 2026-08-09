using BusTicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTicketing.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(t => t.TicketNumber).HasMaxLength(30).IsRequired();
        builder.Property(t => t.PassengerName).HasMaxLength(150).IsRequired();
        builder.Property(t => t.MobileNumber).HasMaxLength(20).IsRequired();
        builder.Property(t => t.NidOrPassport).HasMaxLength(50);
        builder.Property(t => t.Gender).HasMaxLength(20);
        builder.Property(t => t.Remarks).HasMaxLength(500);
        builder.Property(t => t.CancellationReason).HasMaxLength(500);
        builder.Property(t => t.FareAmount).HasColumnType("decimal(9,2)");
        builder.Property(t => t.Status).HasConversion<int>();

        builder.HasIndex(t => t.TicketNumber).IsUnique();

        // The database-level backstop for "prevent duplicate bookings": a seat can be
        // Sold at most once per schedule per travel date. Cancelled tickets are excluded
        // via the filtered index predicate so a cancelled seat can be resold and get a
        // brand-new Ticket row without violating uniqueness. See DATABASE.md.
        builder.HasIndex(t => new { t.ScheduleId, t.TravelDate, t.SeatId })
            .IsUnique()
            .HasFilter("\"Status\" = 0");

        builder.HasOne(t => t.Schedule)
            .WithMany()
            .HasForeignKey(t => t.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Seat)
            .WithMany()
            .HasForeignKey(t => t.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.MobileNumber);
        builder.HasIndex(t => t.TravelDate);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(p => p.Amount).HasColumnType("decimal(9,2)");
        builder.Property(p => p.TransactionRef).HasMaxLength(50).IsRequired();
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.Property(p => p.Method).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();

        builder.HasIndex(p => p.TransactionRef).IsUnique();
        builder.HasIndex(p => p.TicketId).IsUnique();

        builder.HasOne(p => p.Ticket)
            .WithMany()
            .HasForeignKey(p => p.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
