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

## Phase 6: Customer Experience & Business Intelligence — ✅ delivered

### Milestone 8 — QR Code Ticket Generation & Digital Ticket View
- Backend: `IQrCodeService` + `QrCodeService` using QRCoder library generates PNG QR codes
- Backend: `GetTicketQrCodeQuery` returns `TicketQrCodeDto` with base64 QR image and verification payload
- API: `GET /api/v1/booking/tickets/{id}/qrcode` returns QR code data for authenticated users
- Client: "My Tickets" page now has "Show QR" button that opens modal with QR code image
- QR payload encodes ticket number, ID, bus number, seat, and travel date for verification

### Milestone 9 — Email Notifications
- Backend: `IEmailService` abstraction with `SmtpEmailService` implementation using MailKit
- Backend: `EmailSettings` model added to `appsettings.json` with SMTP configuration
- Backend: `SellTicketCommand` and `SellTicketsCommand` extended with optional `Email` field
- Backend: Booking confirmation emails sent asynchronously after ticket sale commits
- Email includes passenger name, ticket number, route, bus, date, departure, seat, and fare
- Frontend: Both admin and client booking forms include optional email field

### Milestone 10 — Advanced Reporting & Analytics
- Backend: `ReportsController` with three endpoints (Admin-only):
  - `GET /api/v1/reports/revenue` — daily revenue summary with occupancy rate
  - `GET /api/v1/reports/occupancy` — per-trip occupancy breakdown
  - `GET /api/v1/reports/top-routes` — top routes by ticket volume and revenue
- DTOs: `RevenueReportDto`, `OccupancyReportDto`, `TopRouteDto`
- All report endpoints support optional `fromDate`, `toDate`, and `routeId` filters

## Phase 7: Printable Tickets & Enhanced Customer Experience — ✅ delivered

### Milestone 11 — Server-Rendered Printable Ticket HTML
- Backend `IPrintTicketService` + `PrintTicketService` generates professional printable HTML for tickets
- API: `GET /api/v1/booking/tickets/{id}/print` returns `text/html` with embedded print stylesheet
- HTML includes company header, ticket number, route/bus/seat/passenger details, fare, status, and auto-print script
- Cancelled tickets include cancellation reason and timestamp in a highlighted box
- Frontend Admin: "Print Ticket" button in booking confirmation step and search results
- Frontend Client: "Print Ticket" button in My Tickets page for active bookings
- Print opens in new browser tab with `window.print()` triggered on load

### Milestone 12 — Print Service Architecture
- `PrintTicketDto` carries all ticket fields plus `PrintableHtml` string
- `GetTicketPrintQueryHandler` loads ticket with Schedule → Bus, Route, and Seller username via join
- Service returns `ContentResult` with `text/html` media type
- Authorization reuses `Permission:BookingViewOwn` so customers, staff, and admin can print
