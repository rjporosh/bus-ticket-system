using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketing.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides identity, audit fields and
/// optimistic-concurrency support via a database-generated row version token.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Optimistic-concurrency token. Deliberately a plain Guid column compared by value,
    /// rather than a native rowversion/xmin type: those are not portable across
    /// PostgreSQL, SQL Server, MySQL and Oracle, and this project's DB provider is a
    /// runtime configuration choice. A SaveChanges interceptor regenerates this value on
    /// every update; EF Core throws
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> when a
    /// stale value is used, which the global exception middleware maps to HTTP 409.
    /// </summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Explicitly excluded from the EF Core model: without [NotMapped], EF Core's
    /// convention-based model builder would try to treat this as a navigation property
    /// (it's a public collection property) and fail at model-build time, because
    /// <see cref="IDomainEvent"/> is an interface and cannot be mapped as an entity type.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
