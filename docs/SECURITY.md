# Security

## Authentication & session model

- **Access tokens**: JWT, HMAC-SHA256 signed, 15-minute default expiry
  (`Jwt:AccessTokenExpiryMinutes`), carrying `sub`, `unique_name`, `email`,
  `role`, `fullName`, and optionally `booth` claims. Validated on every request via
  `TokenValidationParameters` (issuer, audience, signing key, lifetime, 30s clock skew).
- **Refresh tokens**: opaque 64-byte random values (`RandomNumberGenerator`), 7-day
  default expiry, stored server-side per user, **single-use with rotation**: every
  successful refresh revokes the token used and issues a new pair
  (`RefreshTokenCommandHandler`). Replaying an already-revoked token is treated as a
  theft signal — the entire active token chain for that user is revoked immediately.
- **Logout** revokes the specific refresh token supplied; it does not (and cannot,
  statelessly) invalidate an already-issued access token before its natural
  15-minute expiry — a standard, documented tradeoff of JWT bearer auth. Keeping
  the access token lifetime short is the mitigation.

## Password storage

PBKDF2-HMACSHA256, 210,000 iterations (OWASP's 2023+ minimum recommendation for this
algorithm), 16-byte random salt per password, 32-byte derived key, constant-time
comparison (`CryptographicOperations.FixedTimeEquals`) on verification. Iteration
count travels with the hash (`{iterations}.{salt}.{hash}`) so it can be raised in
future without invalidating existing hashes — see `Pbkdf2PasswordHasher`.

**Candidate upgrade**: Argon2id is the stronger modern default for greenfield systems
with no legacy-hash constraint. PBKDF2 was chosen here for zero additional native
dependency; noted in ARCHITECTURE.md as a deliberate, revisitable tradeoff.

## Authorization

Role-based via ASP.NET Core's `[Authorize(Roles = "Admin")]` on a per-endpoint basis
(see API.md for the full matrix). Two system roles are seeded and protected from
modification/deletion at the domain layer (`Role.IsSystemRole`): `Admin`, `BoothStaff`.
Custom roles can be created by an Admin but carry no additional endpoint-level
permissions in this phase (`Role.Description` is descriptive only) — see ROADMAP.md
for planned fine-grained permission claims.

## Token storage on the frontend

The Angular app stores both tokens in `localStorage` (`AuthService`). This is a
**known, documented tradeoff** for an MVP SPA:

| Storage | XSS exposure | CSRF exposure | Survives reload |
|---|---|---|---|
| `localStorage` (current) | Yes — any injected script can read it | No | Yes |
| httpOnly cookie (harder alternative) | No | Yes (needs anti-CSRF token) | Yes |

Given this is an internal booth-staff/admin tool (not a public-facing app taking
arbitrary user content), the primary mitigation is disciplined output encoding
(Angular's default template binding already HTML-escapes interpolated values, so
reflected-XSS surface is low) rather than eliminating the risk architecturally.
**Before this becomes public-facing or handles payment data (Booking/Payment phase),
migrate refresh-token storage to an httpOnly, `SameSite=Strict` cookie** issued by the
API, with the access token kept in memory only (not persisted) — this is called out
explicitly in ROADMAP.md as a prerequisite for the next phase, not an afterthought.

## Transport & headers

- `UseHttpsRedirection()` active outside Development.
- CORS restricted to explicitly configured origins (`Cors:AllowedOrigins`), not
  wildcarded — see DEPLOYMENT.md; the recommended production topology (nginx
  reverse-proxying the SPA and API same-origin) avoids needing CORS at all.
- Standard security headers (HSTS, X-Content-Type-Options, etc.) are not yet
  explicitly configured beyond ASP.NET Core defaults — flagged in ROADMAP.md as a
  pre-production hardening item (e.g. via `NWebsec` or manual middleware).

## Input validation

Every command has a FluentValidation validator executed by `ValidationBehavior`
before the handler runs — no handler trusts unvalidated input. Validation failures
never reach the database layer or throw unhandled exceptions; they return a
structured `Result.Failure(Error.Validation(...))` mapped to HTTP 400 with a
field-level error map.

## Audit trail

Every create/update/status-change/login writes an `AuditLog` row
(`IAuditLogService.LogAsync`) capturing the acting user, action, entity, and
timestamp, in the same database transaction as the business change it records —
see DATABASE.md and ARCHITECTURE.md for why this can never diverge from the actual
change.

## Known gaps for a public-facing / production deployment

Documented explicitly rather than silently absent:

1. No rate limiting on `/auth/login` (brute-force mitigation) — planned via
   `Microsoft.AspNetCore.RateLimiting` in ROADMAP.md.
2. No account lockout after repeated failed logins.
3. No MFA.
4. Refresh token storage should move to httpOnly cookies before public exposure (above).
5. Secrets in `appsettings.Development.json` are placeholder dev-only values —
   verified they are not production-suitable; see DEPLOYMENT.md's checklist.
