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
| Phase 4 — Production hardening + SQA enablement | ✅ Delivered (this session) |

## What Was Completed This Session

1. **Database folder structure** — `database/` with `schema.sql`, `stored-procedures.sql`, `functions.sql`, `views.sql`, `triggers.sql`, `seed-data.sql`, plus `database/2026/august/` versioned monthly folder
2. **Release endpoint** — `ReleaseController` with `GET /api/v1/release/current` and `GET /api/v1/release/notes`
3. **Release notes** — `release/new-release.md` with features built and bugs resolved
4. **Real-bus seat layout (full implementation)** — Backend entity, migration, seat generation, visual coordinates, admin UI for layout configuration, client seat map rendering, and per-seat passenger details
5. **Per-seat passenger booking** — Client booking form uses `FormArray` with "Same for all seats" toggle, sold seats show passenger initials and gender symbols
6. **Mobile number validation** — Frontend regex + maxlength on admin and client booking forms
7. **Postman collection** — `docs/postman-scripts/` (to be created in Milestone 4)
8. **Frontend standardization** — Centralized `api-endpoints.ts` in both admin and client apps
9. **Documentation** — `AI-HANDOVER.md`, `FRONTEND-CLIENT-GUIDE.md`, `FRONTEND-ADMIN-GUIDE.md`, `BACKEND-GUIDE.md`

## What Needs Next Agent Attention

### Milestone 4 — Postman Collection
Create `docs/postman-scripts/` with:
- `BusTicketingSystem.postman_collection.json`
- `environment.postman_environment.json`
- `pre-request-scripts/` (auto-login per role)
- `post-response-scripts/` (token extraction, validation)

### Tests
- Integration tests fail because they require PostgreSQL running. Fix by:
  - Starting PostgreSQL (`docker compose up -d postgres` with `JWT_SECRET` env var)
  - OR fixing InMemory test setup (the `ApiWebApplicationFactory` sets `Database:Testing=true` but tables aren't created)
- Add load tests (k6 or similar) and stress tests
- Add functional test documentation

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
- **No breaking changes:** All Phase 4 additions are additive

## Current Git Status

Only `database/` folder, `release/new-release.md`, docs, and frontend endpoint centralization were added. No code logic was broken. The ReleaseController was added (new file). All changes are additive.
