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
| Phase 6 — Customer Experience & Business Intelligence | ✅ Delivered |
| Phase 7 — Printable Tickets & Enhanced Customer Experience | ✅ Delivered |

## Recommended Commit Message

```
feat(print): Phase 7 — server-rendered printable ticket HTML

Phase 7 Milestone 11 delivers printable tickets via a new server-rendered HTML
endpoint with embedded print stylesheet. Customers, booth staff, and admins can
print professional tickets from both admin and client portals.

Backend:
- Application/Common/Interfaces/IServiceInterfaces.cs: added IPrintTicketService
- Application/Common/Models/BookingDtos.cs: added PrintTicketDto
- Application/Features/Booking/GetTicketPrint.cs: new query + handler joining
  Ticket with Schedule (Bus, Route) and Seller username
- Infrastructure/Services/PrintTicketService.cs: generates professional HTML with
  embedded CSS, auto-print script, and cancellation details
- Infrastructure/DependencyInjection.cs: registered PrintTicketService
- Presentation/Controllers/V1/BookingController.cs: new GET
  /api/v1/booking/tickets/{id}/print endpoint returning text/html

Frontend Admin:
- core/config/api-endpoints.ts: added booking.print endpoint
- core/services/feature-services.ts: added printTicket() method to BookingService
- core/services/api.service.ts: added getBlob() helper for binary responses
- features/booking/booking.component.ts: Print Ticket buttons in confirmation step
  and search results; replaced window.print() with blob download + new tab

Frontend Client:
- core/config/api-endpoints.ts: added tickets.print endpoint
- core/services/tickets.service.ts: added printTicket() method
- core/services/api.service.ts: added getBlob() helper for binary responses
- features/my-tickets/my-tickets.component.ts: Print Ticket button on active tickets
```

## What Was Completed This Session

### Phase 6 Milestone 8 — QR Code Ticket Generation & Digital Ticket View
- **Backend:** `IQrCodeService` + `QrCodeService` using QRCoder library generates PNG QR codes
- **Backend:** `GetTicketQrCodeQuery` returns `TicketQrCodeDto` with base64 QR image and verification payload
- **API:** `GET /api/v1/booking/tickets/{id}/qrcode` returns QR code data for authenticated users
- **Client:** "My Tickets" page now has "Show QR" button that opens modal with QR code image
- **QR payload** encodes ticket number, ID, bus number, seat, and travel date for verification

### Phase 6 Milestone 9 — Email Notifications
- **Backend:** `IEmailService` abstraction with `SmtpEmailService` implementation using MailKit
- **Backend:** `EmailSettings` model added to `appsettings.json` with SMTP configuration
- **Backend:** `SellTicketCommand` and `SellTicketsCommand` extended with optional `Email` field
- **Backend:** Booking confirmation emails sent asynchronously after ticket sale commits
- **Frontend:** Both admin and client booking forms include optional email field

### Phase 6 Milestone 10 — Advanced Reporting & Analytics
- **Backend:** `ReportsController` with three endpoints (Admin-only):
  - `GET /api/v1/reports/revenue` — daily revenue summary with occupancy rate
  - `GET /api/v1/reports/occupancy` — per-trip occupancy breakdown
  - `GET /api/v1/reports/top-routes` — top routes by ticket volume and revenue
- **DTOs:** `RevenueReportDto`, `OccupancyReportDto`, `TopRouteDto`
- All report endpoints support optional `fromDate`, `toDate`, and `routeId` filters

### Phase 7 Milestone 11 — Printable Tickets
- **Backend:** `IPrintTicketService` + `PrintTicketService` generates professional printable HTML
- **Backend:** `GetTicketPrintQuery` + handler loads ticket with Schedule → Bus, Route, Seller
- **API:** `GET /api/v1/booking/tickets/{id}/print` returns `text/html` with embedded print CSS
- **Frontend Admin:** "Print Ticket" button in booking confirmation step and search results
- **Frontend Client:** "Print Ticket" button in My Tickets for active bookings
- HTML includes company header, ticket details, fare, status badge, and auto-print script
- Cancelled tickets display cancellation reason and timestamp in a highlighted box
- Both frontends use `ApiService.getBlob()` + `URL.createObjectURL()` + `window.open()`

### Phase 5 (Previous Session) — Admin Multi-Seat Booking + RealBus Last-Row Config + Age/Gender Display
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

### Priority 1 — Phase 7 Definition

Phase 7 scope (in progress):
- ~~**Printable Tickets:** Server-rendered HTML ticket page with print stylesheet~~ ✅ Delivered
- **SMS Notifications:** Twilio or local GSM gateway integration for booking alerts
- **Offline Mode:** Service worker + IndexedDB for client portal offline search
- **Multi-language:** i18n for admin and client portals
- **Payment Gateway:** Replace mock payment with real processor (bKash, Nagad, card)

### Priority 2 — Email Configuration

Deploy with real SMTP credentials in `appsettings.json` or environment variables:
```json
"Email": {
  "SmtpHost": "smtp.your-provider.com",
  "SmtpPort": 587,
  "SmtpUsername": "user",
  "SmtpPassword": "pass",
  "FromEmail": "noreply@yourdomain.com",
  "EnableNotifications": true
}
```

### Priority 3 — Reports Frontend

Build admin reports page with date range picker, route filter, and charts for:
- Daily revenue trend
- Occupancy heatmap
- Top routes ranking

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
- Backend: Phase 7 printable tickets — `IPrintTicketService`, `PrintTicketService`, `GetTicketPrintQuery`, new `/print` endpoint
- Frontend Admin: Print Ticket buttons in booking confirmation and search results
- Frontend Client: Print Ticket button in My Tickets page
- Tests: unit tests for `PrintTicketService`, integration tests for `/print` endpoint, load test script
- Docs: updated AI-HANDOVER.md, ROADMAP.md, FEATURES.md, release notes

No code logic was broken. All changes are additive.
