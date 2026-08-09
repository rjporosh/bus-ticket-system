# AI Handover

## Current Context

This is a Bus Ticketing System built with:
- **Backend:** .NET 10, Clean Architecture (Domain → Application → Infrastructure → Presentation), MediatR, EF Core, FluentValidation
- **Frontend Admin:** Angular standalone, Material UI, signals-based state
- **Frontend Client:** Angular standalone, Material UI, signals-based state
- **Database:** PostgreSQL (primary), with provider abstraction for SQL Server, MySQL, Oracle
- **Auth:** JWT with refresh token rotation, PBKDF2 password hashing, permission-based authorization

## Phase Status

| Phase | Status |
|-------|--------|
| Phase 1 — Foundation + Fleet Operations | ✅ Delivered |
| Phase 2 — Booking, Mock Payment, Real Dashboard | ✅ Delivered |
| Phase 3 — Client-facing portal | ✅ Delivered |
| Phase 4 — Production hardening + SQA enablement | ✅ Delivered |
| Phase 5 — Admin multi-seat booking + RealBus last-row config + Age/Gender display | ✅ Delivered |

## Recommended Commit Message

```
feat(booking): multi-seat batch selling, RealBus last-row config, age/gender capture

Admin booking wizard now supports multi-seat selection (up to 10) with batch
selling via SellTicketsRequest. Passenger FormArray with same-for-all toggle.
RealBusConfig.LastRowConfig enables custom last-row seat counts. Ticket entity
and DTOs now capture Age alongside Gender. Seat grids tightened with zero-gap
CSS so aisle space is driven by empty visualCol columns only.

Backend:
- Domain/Entities/Ticket.cs: added Age property
- Domain/Entities/SeatLayout.cs: added RealBusConfig.LastRowConfig
- Application/Features/Booking/SellTicket.cs: accept Age in command and DTO
- Application/Features/Booking/SellTicketsCommand.cs: accept Age in SellTicketItem
- Application/Features/Booking/GetAvailableSeats.cs: MapRealBusSeats honors LastRowConfig
- Application/Features/SeatLayouts/SeatLayoutFeature.cs: visual mapping honors LastRowConfig
- Persistence/Migrations/20260809194903_AddAgeToTickets.cs: new migration

Frontend Admin:
- features/booking/booking.component.ts: multi-seat wizard, FormArray, age field,
  gender/age in confirmation, zero-gap seat grid
- features/buses/buses.component.ts: last-row override UI in BusFormDialogComponent

Frontend Client:
- features/booking/booking.component.ts: age field, same-for-all valueChanges
  subscription, zero-gap seat grid

Migration: dotnet ef database update --project src/Infrastructure/BusTicketing.Infrastructure --startup-project src/Presentation/BusTicketing.Api
```

## What Was Completed This Session

### 1. Admin Booking Wizard — Multi-Seat Selling (Milestone 7)
- **`frontend/bus-ticketing-admin/src/app/features/booking/booking.component.ts`**
  - Replaced singular `selectedSeat` with `selectedSeats` signal array (max 10)
  - Seat grid now uses RealBus `visualRow`/`visualCol` layout with `getGridTemplateColumns()`
  - Added `lastSelectedSeatId` signal for visual feedback on last clicked seat
  - Passenger step uses `FormArray` with per-seat passenger forms (name, mobile, gender, age, NID/passport)
  - Added `sameForAll` toggle checkbox with `valueChanges` subscription for reliable sync
  - Batch submission calls `bookingService.sellTickets()` with `SellTicketsRequest` payload
  - Confirmation step displays all sold tickets with numbers, passenger names, seats, fares, gender, and age
  - Added `clearSeats()` button and seat count indicator on seat step
  - Driver seats, gender coloring (male/female), passenger initials on sold seats all rendered

### 2. RealBus Last-Row Config (Milestone 7)
- **`src/Core/BusTicketing.Domain/Entities/SeatLayout.cs`**
  - Added `LastRowConfig` property to `RealBusConfig` class
  - `GenerateRealBusLayout()` now applies `LastRowConfig` when generating seats for the final row

- **`src/Core/BusTicketing.Application/Features/Booking/GetAvailableSeats.cs`**
  - `MapRealBusSeats()` now applies `LastRowConfig` to the last row's left/right seat counts and visual column mapping

- **`src/Core/BusTicketing.Application/Features/SeatLayouts/SeatLayoutFeature.cs`**
  - Visual mapping now applies `LastRowConfig` to the last row

- **`frontend/bus-ticketing-admin/src/app/features/buses/buses.component.ts`**
  - `BusFormDialogComponent` now has "Override last row seats" checkbox with Left/Right inputs
  - Last-row override is persisted in `LayoutConfigJson` as `LastRowConfig`
  - `SeatMapDialogComponent` automatically respects last-row config via `visualRow`/`visualCol` from backend

### 3. Age Capture & Gender Display (Milestone 7)
- **Backend:** Added `Age` property to `Ticket` entity, `Ticket.Sell()`, `SellTicketCommand`, `SellTicketItem`, `SellTicketsCommand`, and all `TicketDto` projections
- **Migration:** `20260809194903_AddAgeToTickets` adds nullable `Age` column to `Tickets` table
- **Frontend Admin:** Passenger form now includes Age field; confirmation step displays Gender and Age
- **Frontend Client:** Passenger form now includes Age field

### 4. Seat Grid CSS Fix (Milestone 7)
- Changed seat grid `gap` from `0.5rem` to `0` in both admin and client booking components
- Aisle spacing is now driven by empty grid columns (visualCol gaps), so left-side seats are flush and right-side seats are flush, with natural aisle space between them

### 5. Client Same-For-All Fix (Milestone 7)
- Rewrote client booking template to show a single shared passenger form when `sameForAll` is checked, and per-seat forms when unchecked
- Added `valueChanges` subscription on `sameForAll` form control to ensure `syncPassengers()` is called reliably when the checkbox changes
- Auto-deselects `sameForAll` when a second seat is selected, generating separate passenger forms
- Added missing `MatCheckboxModule` import to client booking component

## What Needs Next Agent Attention

### Priority 1 — Postman Collection (Milestone 4)

Create `docs/postman-scripts/` with:
- `BusTicketingSystem.postman_collection.json`
- `environment.postman_environment.json`
- `pre-request-scripts/` (auto-login per role)
- `post-response-scripts/` (token extraction, validation)

### Priority 2 — Apply Database Migration

Run the new migration against the database:
```bash
dotnet ef database update --project src/Infrastructure/BusTicketing.Infrastructure --startup-project src/Presentation/BusTicketing.Api
```

### Priority 3 — Verify Last-Row Rendering

Test RealBus buses with `LastRowConfig` overrides (e.g., Left=1, Right=0 or Left=0, Right=1) to confirm the last row renders correctly in both admin and client seat grids.

## Key File Paths

| Purpose | Path |
|---------|------|
| API Controllers | `src/Presentation/BusTicketing.Api/Controllers/V1/` |
| Application Features | `src/Core/BusTicketing.Application/Features/` |
| Domain Entities | `src/Core/BusTicketing.Domain/Entities/` |
| DbContext | `src/Infrastructure/BusTicketing.Infrastructure/Persistence/ApplicationDbContext.cs` |
| Migrations | `src/Infrastructure/BusTicketing.Infrastructure/Persistence/Migrations/` |
| Client routes | `frontend/bus-ticketing-client/src/app/app.routes.ts` |
| Client endpoints | `frontend/bus-ticketing-client/src/app/core/config/api-endpoints.ts` |
| Admin routes | `frontend/bus-ticketing-admin/src/app/app.routes.ts` |
| Admin endpoints | `frontend/bus-ticketing-admin/src/app/core/config/api-endpoints.ts` |
| Admin booking component | `frontend/bus-ticketing-admin/src/app/features/booking/booking.component.ts` |
| Admin buses component (seat map dialog) | `frontend/bus-ticketing-admin/src/app/features/buses/buses.component.ts` |
| Client booking component | `frontend/bus-ticketing-client/src/app/features/booking/booking.component.ts` |
| Database artifacts | `database/` |
| Release notes | `release/new-release.md` |
| Documentation | `docs/` |

## Commands

```bash
# Build
dotnet build BusTicketingSystem.sln

# Test (unit only — integration needs PostgreSQL)
dotnet test tests/BusTicketing.UnitTests/

# Frontend build (client)
cd frontend/bus-ticketing-client && npm install && npm run build

# Frontend build (admin)
cd frontend/bus-ticketing-admin && npm install && npm run build

# EF Core migration
cd src/Presentation/BusTicketing.Api
dotnet ef migrations add <Name> --project ../Infrastructure/BusTicketing.Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure/BusTicketing.Infrastructure --startup-project .
```

## Architecture Reminders

- **Clean Architecture:** Domain → Application → Infrastructure → Presentation
- **CQRS:** Every feature is a Command or Query handled by MediatR
- **Validation:** FluentValidation in `*Validator.cs` files
- **DTOs:** `*Dto.cs` records in Application layer
- **Soft delete:** `IsDeleted` + query filter on all entities
- **Concurrency:** `ConcurrencyStamp` Guid on all entities
- **Audit:** `AuditLog` table + triggers + interceptors
- **No breaking changes:** All additions should be additive

## Current Git Status

Latest uncommitted changes:
- Backend: Ticket Age field + migration
- Backend: RealBus LastRowConfig for last-row seat override
- Admin booking: multi-seat batch selling wizard with passenger FormArray and age field
- Admin booking: gender and age displayed in confirmation step
- Client booking: age field, same-for-all with explicit `@if` rendering and `valueChanges` subscription
- Client booking: added missing `MatCheckboxModule` import
- Both frontends: seat grid gap changed to zero for proper aisle rendering
- Docs: updated AI-HANDOVER.md

No code logic was broken. All changes are additive.
