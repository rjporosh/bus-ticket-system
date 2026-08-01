using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BusTicketing.Infrastructure.Persistence;

/// <summary>
/// Stamps audit fields (created/modified by + timestamps), rotates each changed
/// entity's <see cref="BaseEntity.ConcurrencyStamp"/>, and dispatches collected
/// domain events via MediatR immediately after a successful SaveChanges.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;
    private readonly IPublisher _publisher;

    public AuditableEntityInterceptor(ICurrentUserService currentUser, IDateTimeProvider dateTime, IPublisher publisher)
    {
        _currentUser = currentUser;
        _dateTime = dateTime;
        _publisher = publisher;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = _dateTime.UtcNow;
        var actor = _currentUser.Username ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.CreatedBy = actor;
                    entry.Entity.ConcurrencyStamp = Guid.NewGuid();
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAtUtc = now;
                    entry.Entity.ModifiedBy = actor;
                    entry.Entity.ConcurrencyStamp = Guid.NewGuid();
                    break;

                case EntityState.Deleted:
                    // Soft delete: never physically remove a row.
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = now;
                    entry.Entity.DeletedBy = actor;
                    break;
            }
        }
    }

    private async Task DispatchDomainEventsAsync(DbContext context)
    {
        var entitiesWithEvents = context.ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var events = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();
        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in events)
            await _publisher.Publish(domainEvent);
    }
}
