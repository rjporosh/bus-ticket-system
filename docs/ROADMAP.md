# Roadmap

## Phase 1: Foundation + Fleet Operations — ✅ delivered

Auth, Users, Roles, Stations, Routes, Buses, Seat Layouts, Schedules — fully
implemented backend-to-frontend, with tests.

## Phase 2: Booking, Mock Payment, Real Dashboard — ✅ delivered

Ticket sell/cancel/search, mock payment capture, double-booking prevention
(application pre-check + DB unique-index backstop), and a Dashboard driven by real
sold/available/revenue data. See [FEATURES.md](FEATURES.md) for the line-by-line
checklist and `git log` for the exact commits.

## Use case diagram — current scope (Phase 1 + 2)

```mermaid
flowchart LR
    Admin((Admin))
    Staff((Booth Staff))

    Admin --> UC1[Manage Users & Roles]
    Admin --> UC2[Manage Stations & Routes]
    Admin --> UC3[Manage Fleet & Seat Layouts]
    Admin --> UC4[Manage Schedules]
    Staff --> UC5[View Dashboard]
    Staff --> UC6[View Buses / Routes / Stations]
    Staff --> UC7[Sell Ticket]
    Staff --> UC8[Cancel Ticket]
    Staff --> UC9[Search Ticket]
    Admin --> UC5
    Admin --> UC6
    Admin --> UC7
    Admin --> UC8
    Admin --> UC9

    UC3 -.includes.-> UC3a[Generate Seat Layout]
    UC4 -.includes.-> UC4a[Prevent Bus Double-Booking]
    UC7 -.includes.-> UC7a[Capture Mock Payment]
    UC7 -.includes.-> UC7b[Prevent Seat Double-Booking]
    UC8 -.includes.-> UC8a[Refund Mock Payment]
```

## Activity diagram — creating a bus (illustrates the transactional seat-layout generation)

```mermaid
flowchart TD
    Start([Admin submits Add Bus form]) --> Validate{Valid input?}
    Validate -- no --> ShowErrors[Show field errors] --> Start
    Validate -- yes --> CheckDup{Registration number\nalready exists?}
    CheckDup -- yes --> Conflict[Return 409 Conflict] --> End1([End])
    CheckDup -- no --> BeginTx[Begin DB transaction]
    BeginTx --> CreateBus[Create Bus entity]
    CreateBus --> GenLayout[Generate SeatLayout\nRows x Columns]
    GenLayout --> AssignLayout[Bus.AssignSeatLayout\nvalidates seat count matches]
    AssignLayout --> Persist[Insert Bus + SeatLayout + all Seats]
    Persist --> Commit[Commit transaction]
    Commit --> Success[Return 201 with BusDto]
    Success --> End2([End])
```

## Phase 3: Client-facing portal — ✅ delivered

Separate Angular client portal for public trip search and booking, running
parallel to the admin console. See `frontend/bus-ticketing-client/` for the
full implementation.

### Hardening
- Rate limiting on `/auth/login` (SECURITY.md).
- Fine-grained permission claims per role, not just role-name checks.
- Per-provider migrations actually generated and tested against SQL
  Server/MySQL/Oracle, not just PostgreSQL (DATABASE.md documents the process;
  it hasn't been exercised against all four engines).
- Ticket-number generation moved from a COUNT-based query to a DB sequence per
  date, closing the narrow race condition documented in `SellTicket.cs`.
- Standard security headers (HSTS, X-Content-Type-Options, etc.) explicitly configured.
