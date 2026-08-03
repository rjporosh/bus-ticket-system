using BusTicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext so the Application layer depends only on
/// this interface, never on Microsoft.EntityFrameworkCore's concrete DbContext or on
/// any specific database provider. Infrastructure implements this against whichever
/// provider (PostgreSQL/SQL Server/MySQL/Oracle) is selected in configuration.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Station> Stations { get; }
    DbSet<Domain.Entities.Route> Routes { get; }
    DbSet<Bus> Buses { get; }
    DbSet<SeatLayout> SeatLayouts { get; }
    DbSet<Seat> Seats { get; }
    DbSet<Schedule> Schedules { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Begins a database transaction for handlers that need multi-step atomicity beyond a single SaveChanges call.</summary>
    Task<IAppDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>Provider-agnostic wrapper over EF Core's IDbContextTransaction.</summary>
public interface IAppDbTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
