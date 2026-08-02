using BusTicketing.Domain.Common;

namespace BusTicketing.Domain.Entities;

/// <summary>
/// An immutable record of a business-meaningful action (login, create, update, cancel...).
/// Distinct from Serilog's HTTP request logs: this is queryable, structured, and kept
/// indefinitely for compliance, whereas request logs are operational and may be pruned.
/// </summary>
public class AuditLog : BaseEntity
{
    public string Action { get; private set; } = default!;
    public string EntityName { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string? Details { get; private set; }
    public Guid? PerformedByUserId { get; private set; }
    public string PerformedByUsername { get; private set; } = default!;
    public DateTimeOffset OccurredAtUtc { get; private set; }

    private AuditLog() { } // EF Core

    public static AuditLog Create(
        string action, string entityName, string entityId, string? details,
        Guid? performedByUserId, string performedByUsername, DateTimeOffset occurredAtUtc)
    {
        return new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            PerformedByUserId = performedByUserId,
            PerformedByUsername = performedByUsername,
            OccurredAtUtc = occurredAtUtc
        };
    }
}
