# Roadmap

## Phase 1: Foundation + Fleet Operations — ✅ delivered

Auth, Users, Roles, Stations, Routes, Buses, Seat Layouts, Schedules — fully
implemented backend-to-frontend, with tests.

## Phase 2: Booking, Mock Payment, Real Dashboard — ✅ delivered

Ticket sell/cancel/search, mock payment capture, double-booking prevention
(application pre-check + DB unique-index backstop), and a Dashboard driven by real
sold/available/revenue data.

## Phase 3: Client-facing portal — ✅ delivered

Separate Angular client portal for public trip search and booking, running
parallel to the admin console.

## Phase 4: Production hardening + SQA enablement — ✅ delivered

Database artifacts, release tracking, configurable real-bus seat layout, Postman collection, and enterprise-grade developer documentation.

### Milestones completed in Phase 4
- **Milestone 1 — Database Artifacts:** schema, stored procedures, functions, views, triggers, seed data
- **Milestone 2 — Release Management:** `/release/current` and `/release/notes` endpoints, `release/new-release.md`
- **Milestone 3 — Configurable Real-Bus Seat Layout:** `LayoutType` + `LayoutConfigJson`, per-row left/right counts, visual coordinates
- **Milestone 4 — Postman Collection:** environments, pre-request scripts (auto-login), post-response scripts, example requests for all endpoints

## Phase 5: Admin Multi-Seat Booking + RealBus Last-Row Config + Age/Gender Display — ✅ delivered

### Milestone 5 — Admin Multi-Seat Batch Selling
- Admin booking wizard upgraded from single-seat to multi-seat selection (up to 10 seats)
- Seat grid renders RealBus layout via `visualRow`/`visualCol` CSS grid positioning
- Passenger step uses `FormArray` with per-seat passenger forms (name, mobile, gender, age, NID/passport)
- "Same passenger for all seats" toggle collapses multi-seat bookings to a single form
- Batch submission calls `BookingService.sellTickets()` with `SellTicketsRequest`
- Confirmation step displays all sold tickets (ticket number, passenger, seat, fare, gender, age)
- Driver seats rendered with bus icon; sold seats show passenger initials and gender coloring

### Milestone 6 — RealBus Last-Row Configuration
- `RealBusConfig.LastRowConfig` added to backend entity
- `GenerateRealBusLayout()` applies last-row override when generating seats
- `MapRealBusSeats()` and `SeatLayoutFeature` visual mapping honor `LastRowConfig`
- Admin bus form dialog exposes "Override last row seats" checkbox with Left/Right inputs
- Last-row config persisted in `LayoutConfigJson` and rendered correctly in seat maps

### Milestone 7 — Age Capture + Seat Grid Polish
- `Ticket` entity and all DTOs (`TicketDto`, `SellTicketItem`) extended with `Age` field
- EF Core migration `20260809194903_AddAgeToTickets` adds nullable `Age` column
- Passenger forms in both admin and client include Age input
- Confirmation steps display Gender and Age
- Seat grid `gap` changed to `0`; aisle space driven by empty `visualCol` columns
- Client same-for-all rewritten with explicit `@if` branches for reliable rendering
- Added missing `MatCheckboxModule` import to client booking component
