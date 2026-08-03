# Setup

## Prerequisites

- .NET 10 SDK
- Node.js 22+ and npm
- Docker + Docker Compose (recommended — avoids installing PostgreSQL locally)
- Angular CLI: `npm install -g @angular/cli` (or use the local `npx ng` via the
  project's `devDependencies`)

## Option A — Docker Compose (fastest)

```bash
cd BusTicketingSystem
export JWT_SECRET=$(openssl rand -base64 48)   # PowerShell: $env:JWT_SECRET = ...
docker compose up --build
```

This starts PostgreSQL, the API (port 8080), and the Angular app served by nginx
(port 4200, reverse-proxying `/api` to the API container — no CORS configuration
needed in this mode). The API seeds the reference dataset on first startup.

- API docs: http://localhost:8080/scalar/v1 *(only when `ASPNETCORE_ENVIRONMENT=Development`; the compose file runs `Production` by default — see DEPLOYMENT.md to flip it for local exploration)*
- Health: http://localhost:8080/health/live, http://localhost:8080/health/ready
- Frontend: http://localhost:4200

## Option B — Bare metal

### 1. Database

Run PostgreSQL locally, or point `ConnectionStrings:Default` in
`src/Presentation/BusTicketing.Api/appsettings.Development.json` at any of the four
supported engines (see DATABASE.md for switching providers).

### 2. Backend

```bash
cd src/Presentation/BusTicketing.Api
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
dotnet restore ../../../BusTicketingSystem.sln
dotnet run
```

The app seeds data automatically (`SeedData:Enabled: true` in
`appsettings.Development.json`). Runs at `http://localhost:5000` per
`Properties/launchSettings.json`, opening `/scalar/v1` automatically.

### 3. Frontend

```bash
cd frontend/bus-ticketing-admin
npm install
npm start
```

Runs at `http://localhost:4200`, calling the API at `http://localhost:5000/api/v1`
per `src/environments/environment.ts`. The `authInterceptor` handles attaching the
bearer token and silently refreshing it on 401s — no manual token management needed
during development.

## Seeded accounts

| Username | Password | Role | Booth |
|---|---|---|---|
| `admin` | `Admin@12345` | Admin | — |
| `dhaka_staff_1` | `Dhaka@12345` | BoothStaff | Dhaka |
| `ctg_staff_1` | `Ctg@123456` | BoothStaff | Chittagong |

**Rotate or remove these before any non-local deployment** — see SECURITY.md.

## Running tests

```bash
dotnet test BusTicketingSystem.sln
```

Unit tests need nothing running. Integration tests spin up the real API pipeline
against EF Core's InMemory provider via `WebApplicationFactory<Program>` — no
database or network access required.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `Missing "Jwt:Secret" configuration value` at startup | Set it via `dotnet user-secrets` (dev) or the `Jwt__Secret` env var (containers) — never commit a real secret to `appsettings.json`. |
| 401 immediately after a successful login in the SPA | Clock skew between browser and API host beyond the 30s tolerance in `TokenValidationParameters.ClockSkew`; sync system clocks. |
| `dotnet ef` commands fail with "no database provider configured" | Run EF Core CLI commands from the API project so `Program.cs`'s configuration binding resolves; see the exact command in DATABASE.md. |
