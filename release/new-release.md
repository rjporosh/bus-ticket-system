# Release Notes

## Version: 1.0.3
**Release Date:** 10-08-2026
**Phase:** Phase 5 — Admin Multi-Seat Booking + RealBus Last-Row Config + Age/Gender Display

---

## Features Built

### Milestone 7 — Age Capture + Seat Grid Polish
- **Backend:** `Ticket` entity extended with `Age` property; `Ticket.Sell()`, `SellTicketCommand`, `SellTicketItem`, `SellTicketsCommand`, and all `TicketDto` projections updated
- **Migration:** `20260809194903_AddAgeToTickets` adds nullable `Age` column to `Tickets` table
- **Admin:** Passenger form includes Age input; confirmation step displays Gender and Age
- **Client:** Passenger form includes Age input
- **Frontend (both):** Seat grid `gap` changed to `0`; aisle spacing driven by empty `visualCol` columns
- **Client:** Same-for-all checkbox fixed with explicit `@if` template branches and `valueChanges` subscription; added missing `MatCheckboxModule` import

### Milestone 6 — RealBus Last-Row Configuration
- **Backend:** `RealBusConfig.LastRowConfig` added; `GenerateRealBusLayout()`, `MapRealBusSeats()`, and `SeatLayoutFeature` visual mapping honor last-row override
- **Admin:** Bus form dialog exposes "Override last row seats" checkbox with Left/Right inputs; last-row config serialized to `LayoutConfigJson`

### Milestone 5 — Admin Multi-Seat Batch Selling
- **Admin:** Booking wizard upgraded from single-seat to multi-seat selection (up to 10 seats)
- **Admin:** Seat grid renders RealBus layout via `visualRow`/`visualCol` CSS grid positioning
- **Admin:** Passenger step uses `FormArray` with per-seat passenger forms (name, mobile, gender, age, NID/passport)
- **Admin:** "Same passenger for all seats" toggle collapses multi-seat bookings to a single form
- **Admin:** Batch submission via `BookingService.sellTickets()` with `SellTicketsRequest`
- **Admin:** Confirmation step displays all sold tickets with ticket number, passenger, seat, fare, gender, and age

## Bugs Resolved

| Bug | Resolution |
|-----|-----------|
| Client "Same for all seats" checkbox not functional | Rewrote template with explicit `@if` branches for same-for-all vs per-seat forms; added `valueChanges` subscription and `MatCheckboxModule` import |
| Last-row seats not rendered correctly in RealBus layout | Added `RealBusConfig.LastRowConfig` and updated all backend visual mapping paths |
| Admin booking only sold one seat at a time | Rewrote wizard to use `selectedSeats` array and batch `sellTickets` API |
| Seat grid had unwanted gap between seats | Changed CSS `gap` to `0`; aisle is now natural empty grid columns |
| No age field on passenger forms | Added `Age` to `Ticket` entity, DTOs, commands, and both frontend forms |
| Gender and age not visible to admin on sold tickets | Confirmation step now shows Gender and Age fields |

## Breaking Changes

**None.** All changes are additive:
- New `Age` column is nullable; existing tickets unaffected
- `LastRowConfig` is optional in `RealBusConfig`; existing buses without it behave identically
- Admin booking wizard is a drop-in replacement; existing single-seat flow still works

## Migration Notes

### Database
```bash
dotnet ef migrations add AddAgeToTickets \
  --project src/Infrastructure/BusTicketing.Infrastructure \
  --startup-project src/Presentation/BusTicketing.Api

dotnet ef database update \
  --project src/Infrastructure/BusTicketing.Infrastructure \
  --startup-project src/Presentation/BusTicketing.Api
```

### Frontend
No migration needed. New fields are additive to existing forms.

---

## Previous Versions


## Features Built

### Milestone 6 — CORS Hardening & Admin Seat Map UX
- **Backend:** CORS policy now reads `AllowedOrigins` from `appsettings.json` (`Cors:AllowedOrigins`) and applies them via `WithOrigins()`, replacing the previous blanket `AllowAnyOrigin()` wildcard. The `docker-compose.yml` `Cors__AllowedOrigins__0` environment variable is now actually consumed.
- **Admin:** Seat map dialog reimagined as a bus-body visualization with FRONT/REAR indicators, aligned row labels (A/B/C/D), and a driver-seat icon. Both Standard Grid and Real Bus layouts render inside a unified bus-shaped container.
- **Admin:** RealBus per-row left/right configuration supports arbitrary last-row layouts (e.g., 2+2 with a 5-seat rear row) for accurate real-world bus shapes.

### Milestone 5 — RealBus Layout Completion & Per-Seat Passenger Details
- **Backend:** `GetAvailableSeatsQueryHandler` now computes accurate `visualRow`/`visualCol` for both `StandardGrid` and `RealBus` layouts
- **Backend:** `SeatAvailabilityDto` extended with `PassengerName` and `PassengerGender` so sold seats carry passenger metadata
- **Backend:** `CreateBusCommandHandler` derives `TotalSeats` from generated layout seat count instead of `rows * columns`, preventing mismatch when driver seat or custom row configs are used
- **Backend:** `Bus` entity gains `SetTotalSeats` to allow post-generation correction
- **Admin:** Bus creation form exposes `LayoutType` dropdown (Standard Grid / Real Bus) and serializes `RealBusConfig` to `LayoutConfigJson`
- **Admin:** RealBus config panel includes driver seat toggle, aisle gap input, and per-row left/right seat count inputs
- **Admin:** Seat map dialog renders RealBus layouts using visual coordinates with driver seat styling and dynamic grid columns
- **Client:** Seat map uses `visualRow`/`visualCol` CSS grid positioning for both layout types, producing a real bus shape (left block, aisle, right block, driver seat)
- **Client:** Sold seats display passenger initials and gender symbols (♂ blue / ♀ pink) with color-coded backgrounds
- **Client:** Booking form replaced with `FormArray`-based per-seat passenger form
  - "Same for all seats" toggle defaults to checked, collapsing multi-seat bookings to a single passenger form
  - When unchecked, each selected seat gets its own passenger name, mobile, gender, and NID fields
  - Submit builds `SellTicketsRequest` with per-item passenger data
- **Validation:** Mobile number input enforces numbers-only and max 11 digits on both admin and client booking forms (`Validators.pattern('^[0-9]{0,11}$')` + `maxlength="11"`)

### Milestone 4 — Postman Collection
- `docs/postman-scripts/` folder created
- Environment variables: `base_url`, `customer_token`, `admin_token`, `staff_token`
- Pre-request scripts:
  - Auto-login and store tokens by role
  - Token refresh on 401
  - Dynamic variable substitution
- Post-response scripts:
  - Extract ticket IDs for chained requests
  - Validate response schema
- Example requests for all endpoints:
  - Auth (login, register, refresh, logout)
  - Users (CRUD)
  - Roles (CRUD)
  - Stations (CRUD)
  - Routes (CRUD)
  - Buses (CRUD + seat layout)
  - Schedules (CRUD + trip search)
  - Booking (sell, batch sell, cancel, search, my-tickets, available seats)
  - Dashboard (summary)
  - Release (current, notes)

### Milestone 3 — Configurable Real-Bus Seat Layout
- **Backend:** `SeatLayout` entity extended with `LayoutType` enum (`StandardGrid`, `RealBus`) and `LayoutConfigJson` for row-by-row seat-group configuration
- **Backend:** Seat generation updated to support driver seat, left/right groups per row, and configurable aisle gaps when `RealBus` is selected
- **Backend:** `SeatAvailabilityDto` extended with optional `VisualRow`, `VisualCol`, and `IsDriver` for frontend rendering
- **Frontend (Client):** Booking component now renders a **real-bus-shaped seat grid** between trip summary and passenger form
  - Driver seat icon at front-left
  - Configurable 2+2 or 2+1 seating with center aisle gap
  - Clickable seat selection (up to 10 seats)
  - Visual states: available, selected, sold, out-of-service
- **Migration:** New EF Core migration `20260809000000_AddRealBusSeatLayout` adding `LayoutType` and `LayoutConfigJson` columns; existing data defaults to `StandardGrid` (backward compatible)

### Milestone 2 — Release Management
- `GET /api/v1/release/current` — Public endpoint returning structured release info (version, features, bugs resolved, markdown notes) for SQA team
- `GET /api/v1/release/notes` — Returns raw markdown of release notes
- `release/new-release.md` — Single source of truth for release features and bug resolutions

### Milestone 1 — Database Artifacts
- `database/schema.sql` — Complete PostgreSQL DDL for all entities, indexes, and constraints
- `database/stored-procedures.sql` — Core booking and reporting procedures (`sp_sell_ticket`, `sp_sell_tickets_batch`, `sp_cancel_ticket`, `sp_get_available_seats`, `sp_search_tickets`, `sp_get_dashboard_summary`)
- `database/functions.sql` — Scalar and table functions (`fn_get_seat_availability`, `fn_calculate_fare`, `fn_get_ticket_count_by_date`, `fn_is_seat_sold`, `fn_get_user_permissions`)
- `database/views.sql` — Reporting views (`vw_available_trips`, `vw_sold_tickets`, `vw_dashboard_summary`, `vw_bus_seat_status`, `vw_route_sales`)
- `database/triggers.sql` — Audit triggers (`trg_audit_ticket_changes`, `trg_log_payment_changes`, `trg_set_modified_timestamp`)
- `database/seed-data.sql` — Reference dataset (roles, permissions, stations, routes, buses, schedules)
- `database/2026/august/` — Versioned monthly folder for August 2026 artifacts

### Frontend Enterprise Standardization
- Centralized API endpoint constants in `src/app/core/config/api-endpoints.ts` (both admin and client)
- Centralized route definitions documented in `app.routes.ts`
- New developer guides created:
  - `docs/FRONTEND-CLIENT-GUIDE.md`
  - `docs/FRONTEND-ADMIN-GUIDE.md`
  - `docs/BACKEND-GUIDE.md`
  - `docs/AI-HANDOVER.md`

---

## Bugs Resolved

| Bug | Resolution |
|-----|-----------|
| CORS policy was overly permissive | `Program.cs` now reads `Cors:AllowedOrigins` from `appsettings.json` and uses `WithOrigins()` instead of `AllowAnyOrigin()` |
| Admin seat map lacked bus-body context | Seat map dialog now renders inside a bus-shaped container with FRONT/REAR indicators and aligned row labels |
| RealBus last-row configuration unclear | Per-row left/right seat counts allow arbitrary row layouts (e.g., 2+2 with a 5-seat rear row) |
| Client booking had no visual seat grid | Added real-bus seat layout rendering with driver seat and aisle gaps |
| Seat layout was uniform grid only | Added `LayoutType` and `LayoutConfigJson` for configurable real-bus shapes |
| No per-seat passenger details in client booking | Replaced single passenger form with `FormArray` + "Same for all seats" toggle |
| Sold seats showed no passenger identity | Seat map now shows passenger initials and gender symbols on sold seats |
| Mobile number had no frontend validation | Added numbers-only regex and max 11 digits on admin and client booking forms |
| `TotalSeats` mismatch for RealBus layouts | `CreateBusCommandHandler` now derives total from generated layout seat count |
| RealBus visual coordinates were broken | `GetAvailableSeatsQueryHandler` now computes proper 2D `visualRow`/`visualCol` mapping |
| No centralized API endpoint registry | Created `api-endpoints.ts` in both admin and client apps |
| Missing release tracking for SQA | Added `/release/current` endpoint and `release/new-release.md` |
| Integration tests required manual DB setup | Documented InMemory provider usage; unit tests run with zero dependencies |
| No developer onboarding docs | Created step-by-step guides for frontend, backend, and database changes |

---

## Breaking Changes

**None.** All changes are backward-compatible:
- New `LayoutType` column defaults to `StandardGrid` for existing rows
- New `/release/*` endpoints are public and additive
- New frontend seat grid is rendered inside existing booking component without altering the booking API
- Per-seat passenger form falls back to single-form behavior when "Same for all seats" is checked

---

## Migration Notes

### Database
```bash
dotnet ef migrations add AddRealBusSeatLayout \
  --project src/Infrastructure/BusTicketing.Infrastructure \
  --startup-project src/Presentation/BusTicketing.Api

dotnet ef database update \
  --project src/Infrastructure/BusTicketing.Infrastructure \
  --startup-project src/Presentation/BusTicketing.Api
```

### Frontend
No migration needed. New seat grid uses existing `SeatAvailabilityDto` fields plus optional visual coordinates.

---

## SQA Checklist

- [ ] CORS allows only configured origins from `appsettings.json`
- [ ] Admin seat map dialog shows FRONT/REAR labels and aligned row letters
- [ ] RealBus last row can be configured with all seats together (no aisle)
- [ ] `GET /api/v1/release/current` returns 200 with version, features, and bugs
- [ ] `GET /api/v1/release/notes` returns markdown
- [ ] Admin can create a bus with `LayoutType = RealBus` and custom `LayoutConfigJson`
- [ ] Client booking page shows driver seat, left/right seat groups, and aisle gap
- [ ] Seat selection limits to 10 seats
- [ ] Sold seats show as unavailable with passenger initials and gender symbol
- [ ] Out-of-service seats show as disabled
- [ ] "Same for all seats" toggle collapses multi-seat booking to one form
- [ ] Unchecked toggle shows per-seat passenger forms
- [ ] Mobile number rejects non-numeric characters and enforces max 11 digits
- [ ] Postman collection imports without errors
- [ ] Pre-request scripts auto-populate customer, admin, and staff tokens
- [ ] All unit tests pass (`dotnet test`)
- [ ] All integration tests pass
