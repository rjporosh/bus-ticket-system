# Release Notes — Auth Response & Localization Fix Pass

**Date:** 2026-08-13

## Fixed

- **Login, Register, and Refresh now return `200 OK` with the full `AuthResponseDto`**
  (`accessToken`, `accessTokenExpiresAtUtc`, `refreshToken`, `refreshTokenExpiresAtUtc`,
  `user`), instead of silently returning `204 No Content` with an empty body. Root cause
  was an overload-resolution bug in `AuthController` where the localizer was passed
  positionally instead of as a named argument, causing the response-mapping helper to bind
  to the wrong extension method. See commit `db6152f`.
- **Localization files (`en.json`, `bn.json`) now load correctly in both the client and
  admin apps** instead of 404ing. Root cause was `angular.json` only copying the `public/`
  folder into the build output, never `src/assets/`. See commit `28bce8c`.
- **`TranslatePipe` no longer reaches into `TranslateService`'s private internals** — it now
  uses the existing public `getSync()` method. Template usage (`{{ 'app.key' | translate }}`)
  is unchanged. See commit `8fe8f9e`.
- **Failed translation-file loads are now logged to the console** instead of failing
  silently, making missing/broken translation files easy to spot in development. See
  commit `afc7bdb`.

## Added

- `app.loginSubtitle` translation key (en/bn) in the client app, in preparation for
  localizing the login screen. Not yet wired into a template — see `ai-handover.md`.

## Verified in this environment

- `npm install` succeeds cleanly for both `bus-ticketing-client` and `bus-ticketing-admin`.
- `ng build --configuration development` succeeds for both apps with zero compile errors.
- `src/assets/i18n/en.json` and `bn.json` are correctly present in both apps' build output
  after the fix (confirmed absent before it).
- Every translation key referenced in either app's templates/services already exists in
  both `en.json` and `bn.json` — no key gaps found.

## Known limitations of this verification pass

- **Production Angular builds** (`ng build --configuration production`) could not be
  completed in this sandbox: the build reaches the font-inlining step and fails on a 403
  from `fonts.googleapis.com`, which is outside this sandbox's network allowlist. This is
  an environment restriction, not a code defect — the same builds passed compilation and
  type-checking cleanly. Must be re-run with normal internet access to get a true
  zero-warning production build confirmation.
- **`dotnet build` / `dotnet test`** could not be run at all in this sandbox (no .NET SDK,
  no nuget.org access). The auth fix is a two-token change with no new symbols, but it has
  not been compiler-verified. Run before deploying.
- **No end-to-end/runtime verification** was performed (no way to run the API and both
  Angular dev servers together here): login/register token issuance, live 200 responses
  for the i18n asset URLs, EN↔BN switching in a running browser, and token persistence were
  all verified by reading code and/or checking build output, not by exercising a running
  system.
- **Localization coverage of the UI is incomplete.** The client login screen (and likely
  the client register screen, admin login screen, shared shell/layout, and language
  switcher) still contain hardcoded English strings not yet wired to `| translate`. This
  was scoped as a full sweep in the original task but only partially audited in this pass.

See `ai-handover.md` for the full breakdown of what's done, what's left, and the exact
prompt to give the next agent to continue this work.

## Files changed

```
src/Presentation/BusTicketing.Api/Controllers/V1/AuthController.cs
frontend/bus-ticketing-client/angular.json
frontend/bus-ticketing-admin/angular.json
frontend/bus-ticketing-client/src/app/core/pipes/translate.pipe.ts
frontend/bus-ticketing-admin/src/app/core/pipes/translate.pipe.ts
frontend/bus-ticketing-client/src/app/core/services/translate.service.ts
frontend/bus-ticketing-admin/src/app/core/services/translate.service.ts
frontend/bus-ticketing-client/src/assets/i18n/en.json
frontend/bus-ticketing-client/src/assets/i18n/bn.json
ai-handover.md
RELEASE_NOTES.md
```
