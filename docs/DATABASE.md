# Database

## Provider abstraction

The active database engine is a single configuration key:

```json
{
  "Database": { "Provider": "PostgreSql" },
  "ConnectionStrings": { "Default": "..." }
}
```

Valid values: `PostgreSql`, `SqlServer`, `MySql`, `Oracle`. The switch lives entirely
in `BusTicketing.Infrastructure.Persistence.Providers.DatabaseProviderExtensions` —
no other file in the codebase references a provider-specific EF Core package. See
`src/Infrastructure/BusTicketing.Infrastructure/Persistence/Providers/DatabaseProviderExtensions.cs`.

## Migrations: the honest limitation

EF Core migrations are **not** portable across database engines — a migration
generated against PostgreSQL bakes in Postgres column types (`text`, `timestamptz`)
and syntax that will fail against SQL Server or Oracle. This is a real constraint of
EF Core, not something this project works around; claiming otherwise would be
dishonest. The practical approach, and what this project follows:

1. Pick the provider you're deploying against.
2. Generate that provider's migrations once:
   ```bash
   dotnet ef migrations add InitialCreate \
     --project src/Infrastructure/BusTicketing.Infrastructure \
     --startup-project src/Presentation/BusTicketing.Api \
     --output-dir Migrations/PostgreSql
   ```
3. Repeat per provider you intend to support in a given environment, into separate
   `Migrations/{Provider}` folders (the `MigrationsAssembly`/`MigrationsHistoryTable`
   calls in `DatabaseProviderExtensions` already namespace each provider's history
   table so they don't collide if multiple providers' migrations ever coexist in the
   same assembly).
4. `Program.cs` calls `Database.MigrateAsync()` at startup (inside `DataSeeder`), so
   whichever provider is configured applies its own migrations automatically.

For local development, PostgreSQL via `docker-compose.yml` is the fastest path and
requires no manual migration step beyond the above, run once.

## Optimistic concurrency

Every entity inherits `ConcurrencyStamp` (`Guid`), mapped with `.IsConcurrencyToken()`
in each `IEntityTypeConfiguration<T>`. A `SaveChangesInterceptor`
(`AuditableEntityInterceptor`) regenerates it on every `Added`/`Modified` entry before
`SaveChangesAsync` runs. If two requests race to update the same row, the second
`SaveChangesAsync` throws `DbUpdateConcurrencyException`, which the global exception
middleware maps to **HTTP 409 Conflict** — see ARCHITECTURE.md for why a portable
Guid column was chosen over each provider's native rowversion mechanism.

## Soft delete

`IsDeleted`/`DeletedAtUtc`/`DeletedBy` on every entity. The interceptor rewrites any
`EntityState.Deleted` entry to `EntityState.Modified` and sets these fields instead of
allowing a physical `DELETE`. Every `IEntityTypeConfiguration<T>` adds
`HasQueryFilter(e => !e.IsDeleted)` so soft-deleted rows are invisible to normal
queries without every handler needing to remember to filter them out.

## Indexing

| Table | Index | Reason |
|---|---|---|
| `Users` | Unique on `Username`, `Email` | Login lookup, uniqueness enforcement |
| `RefreshTokens` | Unique on `Token`; non-unique on `UserId` | Token lookup on refresh; listing a user's sessions |
| `Stations` | Unique on `(Name, City)` | Prevent duplicate stations |
| `Routes` | Unique on `(OriginStationId, DestinationStationId)` | One route per station pair |
| `Buses` | Unique on `Number`, `RegistrationNumber` | Fleet identifiers must be unique |
| `SeatLayouts` | Unique on `BusId` | Enforces the 1:1 Bus↔SeatLayout invariant at the DB level, not just in domain code |
| `Seats` | Unique on `(SeatLayoutId, SeatNumber)` | No duplicate seat codes within one bus |
| `Schedules` | Non-unique on `(BusId, DepartureTime)`, on `RouteId` | Overlap-conflict checks; route lookups |
| `AuditLogs` | Non-unique on `OccurredAtUtc`, on `(EntityName, EntityId)` | Chronological audit views; "history of this entity" queries |

## Preventing duplicate bookings / scheduling conflicts

This phase does not yet implement Booking (see ROADMAP.md), but the same conflict
pattern is already in place for **Schedules**: `CreateScheduleCommandHandler` checks
for an existing active schedule on the same bus with an overlapping `DaysOfWeek` bit
and the same `DepartureTime` before inserting, and returns `Error.Conflict(...)` (HTTP
409) rather than allowing a bus to be double-booked. The unique index on
`(SeatLayoutId, SeatNumber)` and the `SeatLayout.BusId` unique index provide DB-level
backstops for the same class of problem in the seat-layout domain. When Booking is
implemented, seat-level double-booking prevention will follow the identical
pattern — application-level check plus a unique constraint on
`(ScheduleId, TravelDate, SeatId)` as the DB-level backstop — returning 409 on
conflict exactly as `CreateScheduleCommand` does today.

## Transactions

Multi-step writes that must be atomic use `IApplicationDbContext.BeginTransactionAsync`
(see `CreateBusCommandHandler`, which inserts the `Bus`, its `SeatLayout`, and every
generated `Seat` inside one transaction — a bus is never left in a state where it
exists without a complete seat map, even if the process crashes mid-write).
