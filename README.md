# Bus Ticketing System

A production-grade MVP for a bus ticketing back office: fleet, routes, stations,
schedules and seat layouts, built to demonstrate senior-level backend and frontend
engineering practices. Booking, mock payment, and the reporting dashboard module are
scoped for a subsequent phase (see [ROADMAP.md](ROADMAP.md)) — this phase delivers a
complete, working slice rather than a shallow pass across every module.

> Reference scenario: 2 ticket booths (Dhaka, Chittagong), 6 buses, 24 seats each,
> 2 routes, matching the original product brief exactly (seed data reproduces it).

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 Web API, Clean Architecture, Vertical Slices, CQRS (MediatR), FluentValidation, EF Core 10 |
| Database | PostgreSQL, SQL Server, MySQL or Oracle — switchable via configuration only |
| Auth | JWT access tokens + rotating refresh tokens, role-based authorization |
| Frontend | Angular (latest stable — see note below), standalone components, Signals, Angular Material |
| Docs | OpenAPI + Scalar |
| Observability | Serilog, health checks |
| Infra | Docker, Docker Compose |

**A note on requested versions:** .NET 10 is current and used throughout. "Angular 22"
does not exist as of this writing (current stable is Angular 21); the frontend targets
the latest stable Angular release using the same standalone/Signals architecture that
was requested, and should be a trivial `npm` version bump once Angular 22 ships.

## Repository layout

```
BusTicketingSystem/
├── src/
│   ├── Core/
│   │   ├── BusTicketing.Domain/          # Entities, enums, domain events — zero dependencies
│   │   └── BusTicketing.Application/     # CQRS handlers, validators, Result pattern, DTOs
│   ├── Infrastructure/
│   │   └── BusTicketing.Infrastructure/  # EF Core, DB provider switch, JWT, hashing, seeding
│   └── Presentation/
│       └── BusTicketing.Api/             # Controllers, middleware, Program.cs
├── tests/
│   ├── BusTicketing.UnitTests/
│   └── BusTicketing.IntegrationTests/
├── frontend/
│   └── bus-ticketing-admin/              # Angular admin console
├── docs/                                 # This documentation set
├── docker-compose.yml
└── BusTicketingSystem.sln
```

## Quick start

See [SETUP.md](SETUP.md) for full local setup, or the short version:

```bash
# Backend + PostgreSQL + frontend, all in containers
export JWT_SECRET=$(openssl rand -base64 48)
docker compose up --build

# API:      http://localhost:8080/scalar/v1  (Development only)
# Frontend: http://localhost:4200
```

Seeded logins (see [SECURITY.md](SECURITY.md) for why these must be rotated before any
real deployment): `admin`, `dhaka_staff_1`, `ctg_staff_1`.

## Documentation index

| Doc | Contents |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Layering, CQRS flow, design decisions and their tradeoffs, Container/Class/Deployment diagrams |
| [ERD.md](ERD.md) | Entity-relationship diagram and field-level notes |
| [DATABASE.md](DATABASE.md) | Provider abstraction, migrations strategy, indexing, concurrency |
| [API.md](API.md) | Endpoint reference, auth flow sequence diagram, sample requests |
| [SETUP.md](SETUP.md) | Local dev setup (bare metal and Docker) |
| [DEPLOYMENT.md](DEPLOYMENT.md) | Deployment diagram, environment configuration, production checklist |
| [SECURITY.md](SECURITY.md) | Threat model, auth design, hardening checklist |
| [ROADMAP.md](ROADMAP.md) | What's built, what's next (Booking/Payment/Dashboard), use-case and activity diagrams |
| [FEATURES.md](FEATURES.md) | Feature-by-feature checklist against the original brief |

## What is honestly not verified

This was built in an offline sandbox with no .NET SDK, no Node package registry
access, and no Docker daemon. Every file was written and manually cross-checked
(imports, DI wiring, route/component name matches), but **no command in this
document has actually been executed against the real toolchain**. Treat the first
`dotnet build` and `npm install && ng build` you run as the real first build — see
FEATURES.md for the full disclosure.
