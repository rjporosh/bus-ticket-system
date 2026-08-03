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

## Preventing duplicate bookings (implemented in Phase 2)

`Ticket` uniqueness on `(ScheduleId, TravelDate, SeatId)` is enforced at two layers:

1. **Application-level pre-check** in `SellTicketCommandHandler` — provider-agnostic,
   runs before insert.
2. **DB-level backstop** — a unique index filtered to `Status = Sold` (cancelled
   tickets are excluded so a freed seat can be resold under a new `Ticket` row). This
   closes the race condition where two booth staff submit a sale for the same seat
   within milliseconds of each other; the second insert fails with a unique
   violation, caught in `SellTicketCommandHandler` and returned as HTTP 409.

**Honest portability caveat**: filtered/partial unique indexes are natively supported
by PostgreSQL and SQL Server (the `HasFilter(...)` call in `TicketConfiguration`
targets these). MySQL has no equivalent partial-unique-index feature, and Oracle's
approach differs (function-based unique index exploiting NULL-is-distinct
semantics). Since migrations are already per-provider (see above), the MySQL/Oracle
migration for this table should replace the filter with a computed nullable column
(e.g. `ActiveSeatKey`, populated only when `Status = Sold`, `NULL` otherwise) and a
plain unique index on that column — both engines treat multiple `NULL`s as
non-conflicting, achieving the same effect. This is flagged rather than silently
assumed to work identically everywhere; the **application-level pre-check remains
the true provider-agnostic guard** regardless of which DB-level mechanism backs it.

Ticket numbers (`TKT-YYYYMMDD-XXXX`) also carry their own unique index as a second,
independent safety net — see the doc comment on `GenerateTicketNumberAsync` for the
one known (and acceptable, at this scale) race condition in that generator.

## Transactions

Multi-step writes that must be atomic use `IApplicationDbContext.BeginTransactionAsync`
(see `CreateBusCommandHandler`, which inserts the `Bus`, its `SeatLayout`, and every
generated `Seat` inside one transaction — a bus is never left in a state where it
exists without a complete seat map, even if the process crashes mid-write).
