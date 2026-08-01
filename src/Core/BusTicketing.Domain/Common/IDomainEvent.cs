using MediatR;

namespace BusTicketing.Domain.Common;

/// <summary>
/// Marker interface for domain events. Events are collected on aggregates and
/// dispatched via MediatR immediately after a successful SaveChangesAsync,
/// so handlers never observe a half-committed transaction.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredOnUtc { get; }
}
