# Phase 2 & 3 Completion Plan

## Assumptions & Decisions

1. **Shared API continues** — No separate client API controllers. The existing `BookingController` already allows `Customer` role. A new `GET /booking/my-tickets` endpoint will be added for self-service.
2. **Ticket numbers** — Replace COUNT-based generation with a `TicketNumberCounter` table + retry loop. Portable across all four supported providers (PostgreSQL, SQL Server, MySQL, Oracle). No provider-specific sequences needed.
3. **Migrations** — Generate initial EF Core migration in `BusTicketing.Infrastructure/Migrations/`. One migration project, per-provider history tables already configured in `DatabaseProviderExtensions.cs`.
4. **Rate limiting** — Use `AspNetCoreRateLimit` NuGet package, configurable via `appsettings.json`.
5. **Security headers** — Custom middleware adding HSTS, X-Content-Type-Options, X-Frame-Options, Permissions-Policy, Referrer-Policy. Only enforced in non-Development environments (local HTTPS already used).
6. **Fine-grained permissions** — Introduce `Permission` enum + `RolePermission` join entity. Replace `[Authorize(Roles = ...)]` with `[Authorize(Policy = "Permission:Booking.Sell")]` on booking endpoints. Seed default permissions for existing roles.
7. **Payment UI** — Admin booking wizard already auto-captures on sell and auto-refunds on cancel. Add a lightweight Payments tab to admin for manual intervene (capture pending, fail, refund) — useful for the "mock" aspect and testing edge cases.
8. **Client seat selection** — Reuse the same `GetAvailableSeats` endpoint. Client booking component will show the seat grid, allow multi-seat selection (not just single seat like admin), and submit all seats in one `SellTicketCommand` per seat (one API call per seat, or batch if backend supports it — see open question).
9. **Client booking API shape** — Backend `SellTicketCommand` is single-seat. Client will either call it N times for N seats, or we add a batch endpoint. **Decision: add `SellTicketsCommand` for batch booking** so the client can book multiple seats in one transaction.

## Open Question

> Should the client booking flow book one seat at a time (N API calls) or use a new batch `SellTicketsCommand`?

**Recommended answer: Add `SellTicketsCommand`.** It's cleaner for the UX (user selects multiple seats, one click), and the backend already supports transactional multi-seat booking conceptually. It also reduces round-trips and partial-failure scenarios.

## Task Breakdown

### Backend — Phase 2 Hardening

| # | Task | Files to create/modify |
|---|------|----------------------|
| B1 | Add `TicketNumberCounter` entity + `RolePermission` + `Permission` enum | Domain: new entity, new enum |
| B2 | Add DbContext configurations for new entities | Infrastructure: Configurations/ |
| B3 | Replace COUNT-based ticket number with counter table + retry | Application: `SellTicket.cs` |
| B4 | Generate EF Core initial migration | `BusTicketing.Infrastructure/Migrations/` |
| B5 | Add rate-limit policy on `/auth/login` (5 attempts/minute) | Presentation: middleware + config |
| B6 | Add security headers middleware | Presentation: new middleware |
| B7 | Add permission-based authorization policies + seed defaults | Infrastructure: DI + DataSeeder |
| B8 | Replace role-only `[Authorize]` with permission policies on booking/dashboard endpoints | Presentation: BookingController, DashboardController |
| B9 | Add `SellTicketsCommand` (batch booking for client portal) | Application: new command/handler/validator |
| B10 | Add `GetMyTicketsQuery` (customer's own bookings) | Application: new query/handler |
| B11 | Add origin/destination trip search | Application: extend `GetTripsForDateQuery` or add new query |
| B12 | Integration tests: BookingController (sell, cancel, search, 409) | Tests: IntegrationTests |
| B13 | Integration tests: DashboardController | Tests: IntegrationTests |
| B14 | Integration tests: AuthController (rate limit) | Tests: IntegrationTests |

### Frontend Admin — Phase 2 Completion

| # | Task | Files to create/modify |
|---|------|----------------------|
| A1 | Add Payments tab to booking component (list + manual actions) | Admin: booking.component.ts |
| A2 | Wire Payment service calls (capture/refund/fail) | Admin: feature-services.ts |

### Frontend Client — Phase 3 Completion

| # | Task | Files to create/modify |
|---|------|----------------------|
| C1 | Fix `TripDto` and `TicketDto` interfaces to match backend exactly | Client: `api-models.ts` |
| C2 | Add `BookingRequest` matching backend `SellTicketCommand` | Client: `api-models.ts` |
| C3 | Create client feature services (`TripsService`, `BookingService`, `TicketsService`) | Client: `core/services/` |
| C4 | Create client `ToastService` (MatSnackBar wrapper) | Client: `core/services/` |
| C5 | Add `authGuard` for protected routes (`/my-tickets`, `/booking`) | Client: `core/guards/` |
| C6 | Rewrite booking component: real trip fetch, seat grid (multi-select), passenger form, payment step, real submit | Client: `features/booking/` |
| C7 | Rewrite My Tickets: correct status mapping, use TicketsService, Toast notifications | Client: `features/my-tickets/` |
| C8 | Enhance search: add route/station filter dropdowns | Client: `features/search/` |
| C9 | Update shell to show/hide nav based on auth + add "My Tickets" active state | Client: `layout/shell.component.ts` |

### Validation

| # | Task |
|---|------|
| V1 | `dotnet test` (unit + integration) |
| V2 | `dotnet ef migrations add` + `dotnet ef database update` against PostgreSQL |
| V3 | Angular `ng build` for both admin and client apps |
| V4 | Manual smoke test: client registers → searches → books seats → views My Tickets → cancels |

## Risks

- **Migration generation**: `dotnet ef` tool must be available. If not, migrations can be hand-written (the schema is small and stable).
- **Ticket counter race condition**: The retry loop handles the known edge case. Under extreme load (>100 concurrent sales/minute), may need SERIALIZABLE isolation. Documented as a known limit.
- **CORS in Production**: Currently `AllowAnyOrigin`. After hardening, switch to configured origins from `appsettings.json` + env vars.
