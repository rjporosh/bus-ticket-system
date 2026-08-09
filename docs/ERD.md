# Entity Relationship Diagram

```mermaid
erDiagram
    ROLE ||--o{ USER : "assigned to"
    USER ||--o{ REFRESH_TOKEN : "issues"
    STATION ||--o{ ROUTE : "origin of"
    STATION ||--o{ ROUTE : "destination of"
    BUS ||--|| SEAT_LAYOUT : "has exactly one"
    SEAT_LAYOUT ||--o{ SEAT : "contains"
    BUS ||--o{ SCHEDULE : "runs"
    ROUTE ||--o{ SCHEDULE : "used by"

    ROLE {
        guid Id PK
        string Name UK
        string Description
        bool IsSystemRole
    }

    USER {
        guid Id PK
        string Username UK
        string Email UK
        string PasswordHash
        string FullName
        string PhoneNumber
        string BoothName
        bool IsActive
        guid RoleId FK
    }

    REFRESH_TOKEN {
        guid Id PK
        guid UserId FK
        string Token UK
        datetime ExpiresAtUtc
        datetime RevokedAtUtc
        string ReplacedByToken
    }

    STATION {
        guid Id PK
        string Name
        string City
        string Address
        bool IsActive
    }

    ROUTE {
        guid Id PK
        string Name
        guid OriginStationId FK
        guid DestinationStationId FK
        decimal DistanceKm
        int EstimatedDurationMinutes
        bool IsActive
    }

    BUS {
        guid Id PK
        string Number UK
        string RegistrationNumber UK
        string OperatorName
        int TotalSeats
        bool IsActive
    }

    SEAT_LAYOUT {
        guid Id PK
        guid BusId FK "unique"
        int Rows
        int Columns
    }

    SEAT {
        guid Id PK
        guid SeatLayoutId FK
        string SeatNumber "e.g. A1"
        string RowLabel
        int ColumnNumber
        int Class "Economy|Business|Sleeper"
        bool IsActive
    }

    SCHEDULE {
        guid Id PK
        guid BusId FK
        guid RouteId FK
        time DepartureTime
        time ArrivalTime
        int DaysOfWeek "bitflags"
        date EffectiveFrom
        date EffectiveTo "nullable"
        decimal FareAmount
        int Status "Scheduled|InProgress|Completed|Cancelled"
    }

    AUDIT_LOG {
        guid Id PK
        string Action
        string EntityName
        string EntityId
        string Details
        guid PerformedByUserId FK
        string PerformedByUsername
        datetime OccurredAtUtc
    }
```

## Notes

- **Every table** additionally carries the `BaseEntity` audit columns:
  `CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy`, `IsDeleted`,
  `DeletedAtUtc`, `DeletedBy`, `ConcurrencyStamp`. These are omitted above for
  readability — see [DATABASE.md](DATABASE.md) for the full column list.
- **`Bus` ↔ `SeatLayout`** is a strict 1:1 — enforced by a unique index on
  `SeatLayout.BusId` — because a bus is never usable without exactly one seat map.
- **`Schedule`** is a *recurring trip template*, not a per-date row. A concrete
  trip on a specific calendar date is resolved at query time by intersecting
  `DaysOfWeek`/`EffectiveFrom`/`EffectiveTo` against the requested date
  (`Schedule.RunsOn(date)` in the domain layer). This avoids pre-materializing rows
  for every bus/day/year combination — see ARCHITECTURE.md for the tradeoff.
- **`AuditLog`** has no FK constraint enforced on `PerformedByUserId` on purpose:
  audit history must survive a user being deleted.
- Booking/Ticket/Payment tables are intentionally absent from this phase — see
  [ROADMAP.md](ROADMAP.md) for the planned schema addition.
