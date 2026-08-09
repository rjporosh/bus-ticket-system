# Features

Status legend: ✅ implemented and tested/reviewed · ⚠️ implemented, partially · ⏸ deferred to Phase 2 (see ROADMAP.md)

## Backend requirements

| Requirement | Status | Notes |
|---|---|---|
| ASP.NET Core 10 Web API | ✅ | |
| Clean Architecture | ✅ | Enforced by project-reference direction, see ARCHITECTURE.md |
| Vertical Slice Architecture | ✅ | One file per command/query cluster under `Application/Features/{Module}` |
| CQRS | ✅ | Every write is a Command, every read a Query, via MediatR |
| MediatR | ✅ | Pipeline behaviors: validation, logging, performance |
| FluentValidation | ✅ | One validator per command, auto-discovered and run by `ValidationBehavior` |
| Global Exception Middleware | ✅ | `GlobalExceptionMiddleware` → RFC 7807 problem+json |
| Result Pattern | ✅ | `Result`/`Result<T>`/`Error`, used by every handler instead of throwing for expected failures |
| EF Core 10 | ✅ | |
| Dependency Injection | ✅ | Constructor injection throughout; composition root in `Program.cs` + two `DependencyInjection.cs` extension classes |
| Repository abstraction | ✅ | `IApplicationDbContext` — see ARCHITECTURE.md for why this is the correct granularity vs. a generic `IRepository<T>` wrapper |
| DB provider abstraction (Postgres/SQL Server/MySQL/Oracle, config-only switch) | ✅ | `DatabaseProviderExtensions` — see DATABASE.md for the one honest caveat (migrations are per-provider, not shared) |
| JWT Authentication | ✅ | |
| Refresh Tokens | ✅ | Rotating, single-use, reuse-detection revokes the full chain |
| Role-based Authorization | ✅ | `[Authorize(Roles = ...)]` per endpoint |
| API Versioning | ✅ | `Asp.Versioning`, URL-segment style (`/api/v1/...`) |
| OpenAPI + Scalar | ✅ | `/openapi/v1.json`, `/scalar/v1` (Development only) |
| Serilog | ✅ | Console + rolling file sinks, request logging middleware |
| Health Checks | ✅ | `/health/live`, `/health/ready` (DB check) |
| Docker & Docker Compose | ✅ | Multi-stage Dockerfiles for API and frontend, orchestrated compose file |
| Environment-based configuration | ✅ | `appsettings.{Environment}.json` + env var overrides |
| Audit logging | ✅ | `AuditLog` entity + `IAuditLogService`, written in the same transaction as the change |
| Pagination, filtering, searching | ✅ | `PaginatedList<T>` + query params on every list endpoint |
| Optimistic concurrency | ✅ | Portable `ConcurrencyStamp`, see ARCHITECTURE.md for the cross-provider rationale |
| Transaction handling | ✅ | `IApplicationDbContext.BeginTransactionAsync`, used in `CreateBusCommandHandler` |
| Prevent duplicate bookings | ✅ | `SellTicketCommandHandler`: application pre-check + DB unique-index backstop (filtered on Postgres/SQL Server; MySQL/Oracle equivalent documented in DATABASE.md), catches the race via `DbUpdateException` → 409 |
| Return 409 for conflicts | ✅ | `Error.Conflict` → HTTP 409 throughout (duplicate stations/routes/buses, schedule overlaps, concurrency conflicts) |

## Business modules

| Module | Status |
|---|---|
| Authentication | ✅ |
| Users | ✅ |
| Roles | ✅ |
| Buses | ✅ |
| Routes | ✅ |
| Stations | ✅ |
| Schedules | ✅ |
| Seat Layout | ✅ |
| Booking | ✅ Phase 2 — sell/cancel/search, double-booking prevention (application pre-check + DB unique-index backstop), 409 on conflict |
| Mock Payment | ✅ Phase 2 — Pending→Captured/Failed→Refunded state machine, no real processor integrated (by design, per brief) |
| Dashboard | ✅ Phase 2 — real sold/available/revenue numbers, route-wise and bus-wise breakdowns, backed by actual Ticket data |

**Deferred, by explicit decision, not oversight:** a separate anonymous
public-facing client booking portal. The original brief specified two internal
ticket booths operated by staff — it never specified a customer-facing
self-service surface, its identity model, or its payment flow. Building one
speculatively would mean inventing requirements rather than implementing them.
The admin console's Booking tab fully covers the documented staff workflow
(search trip → seat → passenger info → confirm → print). See ROADMAP.md for
what a client portal would need before it could be built responsibly
(primarily: the httpOnly-cookie auth migration in SECURITY.md, since a
public surface has a different threat model than an internal tool).

## Client portal (Phase 3)

| Requirement | Status | Notes |
|---|---|---|
| Separate Angular client app | ✅ | `frontend/bus-ticketing-client/` |
| Public trip search | ✅ | No auth required for browsing |
| Seat selection | ✅ | Interactive real-bus-shaped seat map with driver seat, left/right blocks, aisle gap, and visual coordinates |
| Per-seat passenger details | ✅ | `FormArray` with "Same for all seats" toggle; each seat gets individual name, mobile, gender, NID |
| Passenger gender on seat map | ✅ | Sold seats show initials and ♂/♀ symbols, color-coded blue/pink |
| Booking flow | ✅ | Passenger details + fare calculation + per-seat data submission |
| My Tickets | ✅ | View and cancel bookings |
| Login | ✅ | Optional auth for returning customers |
| Docker + nginx | ✅ | Multi-stage build with SPA fallback |

## Frontend requirements

| Requirement | Status |
|---|---|
| Angular, standalone components | ✅ |
| Signals | ✅ | `AuthService`, `LoadingService`, and every list/dialog component use signals for local state |
| Angular Material | ✅ |
| Responsive design | ⚠️ | Layout uses CSS grid/flexbox with `clamp()`/`auto-fit` for tables and tiles; not pixel-audited against every breakpoint on a real device |
| Lazy loading | ✅ | Every feature route uses `loadComponent()` |
| Route guards | ✅ | `authGuard`, `roleGuard` (Users/Roles restricted to Admin) |
| HTTP interceptor | ✅ | Auth attach + 401 refresh-retry, loading tracking, error toasts — three composed functional interceptors |
| Loading indicators | ✅ | Global top progress bar (`LoadingService` + request interceptor) |
| Toast notifications | ✅ | `ToastService` wrapping `MatSnackBar` |
| Modern enterprise admin dashboard | ✅ | Custom "Dispatch Console" design system — see ARCHITECTURE.md and `theme.scss` for the rationale against a generic SaaS-admin look |

## Documentation deliverables

| Doc | Status |
|---|---|
| README.md | ✅ |
| ARCHITECTURE.md | ✅ (Container, Class, and CQRS sequence diagrams) |
| ERD.md | ✅ (ER diagram) |
| DATABASE.md | ✅ |
| API.md | ✅ (endpoint table + auth sequence diagram) |
| SETUP.md | ✅ |
| DEPLOYMENT.md | ✅ (deployment/flowchart diagram) |
| SECURITY.md | ✅ |
| ROADMAP.md | ✅ (use case + activity diagrams) |
| FEATURES.md | ✅ (this file) |
| Sample API requests | ✅ | `docs/sample-requests.http` |

Diagram coverage against the original request (ER, Use Case, Sequence, Activity,
Class, Deployment, Container): all seven types are present across the docs above.

## What has **not** been executed — read this before trusting a green checkmark

This entire project was built in a sandboxed environment with:
- **No .NET SDK installed** — `dotnet build`/`dotnet test`/`dotnet ef` were never run.
- **No npm registry access** — `npm install`/`ng build` were never run.
- **No Docker daemon** — `docker compose up`/`docker build` were never run.

Every file was hand-written and then cross-checked manually: import statements
matched against declaring packages, DI registrations matched against constructor
dependencies, route/component names matched between `app.routes.ts` and each
component's actual export, controller endpoints matched against Angular service
calls, and — after this conversation's explicit bug report — a full sweep for
missing-package and missing-`using` errors was performed, finding and fixing three
real issues (see the `fix:` commit in git log). That sweep is thorough but it is
still manual review, not a compiler. **Run `dotnet build` and
`npm install && ng build` as the actual first verification** — see SETUP.md.
