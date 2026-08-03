using BusTicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTicketing.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Details).HasMaxLength(2000);
        builder.Property(a => a.PerformedByUsername).HasMaxLength(50).IsRequired();

        builder.HasIndex(a => a.OccurredAtUtc);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
    }
}
