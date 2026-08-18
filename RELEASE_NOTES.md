# Release Notes — Auth Response & Localization Fix Pass

## 2026-08-18 — i18n continuation: auth screens, admin shell, common buttons audit

### Fixed / What testers should check

- **Client login screen (`/login`) is now fully localized.** Switch to Bengali (BN) via
  the language switcher and confirm every visible string changes: page title, subtitle,
  the "Username or Email" and "Password" field labels, both validation messages (try
  submitting empty), the "Sign in" button, and the "Don't have an account? Register now"
  footer. Also try triggering a failed login (wrong password) — the error message under
  the form should be in the selected language too.
- **Client register screen (`/register`) is now fully localized.** Same check as above:
  title/subtitle, all six field labels, every validation message (empty fields, username
  under 3 characters, invalid email, password under 8 characters, mismatched passwords),
  the "Create Account" button, and the "Already have an account? Sign in" footer. On a
  successful registration, the confirmation popup ("Account Created!" / and its body
  text) should also appear in the selected language. On a failed registration, the error
  message should too.
  - **Testers should specifically check the password-length validation message.** It
    previously (in unwired, unused translation data) said "at least 6 characters" in
    both languages even though the form actually requires 8 — this has been corrected to
    say "8 characters" in both English and Bengali. Confirm the message you see matches
    the app's actual rule (8 characters).
- **Admin login screen (`/login`)** — this was already localized; the one remaining gap
  (the fallback error message shown on a failed login when the server doesn't return a
  specific error) is now localized too.
- **Admin shell/sidebar** — the brand title, "Dispatch Console" subtitle, "Sign out" menu
  item, and the account-menu button's accessibility label are now localized. Switch to
  Bengali and confirm the sidebar and top-right user menu change language along with the
  rest of the app.

### Known limitations of this pass — see `ai-handover.md` for full detail

- **Most non-auth screens are still English-only in both apps** — the booking flow,
  trip search, and "My Tickets" in the client app, and schedules/users/booking/
  stations/buses/roles/routes management in the admin app all still have hardcoded
  English text that doesn't respond to the language switcher. This turned out to be a
  much bigger scope than a "common buttons" sweep once audited — see `ai-handover.md`
  section 2.1 for the full breakdown and a prioritized list of what's safe to fix next
  (starting with generic Save/Cancel/Close/Back dialog buttons).
- **Production Angular builds** (`ng build --configuration production`) still could not
  be completed in this development sandbox — same `fonts.googleapis.com` 403 as the
  previous pass, which is a network-allowlist restriction in this environment, not a
  code defect. Development builds (`ng build --configuration development`) succeeded
  with zero errors for both apps, confirming all TypeScript/template changes are sound.
- **`dotnet build` / `dotnet test` still could not be run** — no .NET SDK is available in
  this sandbox and both `api.nuget.org` and Ubuntu's package mirror for the exact
  `dotnet-sdk` version are unreachable here. **The backend auth fix from the previous
  pass (`db6152f`) has now gone through three passes on this repo without ever being
  compiler-verified** — this should be the top priority for whoever has a normal-network
  environment with the .NET SDK available.
- **No end-to-end/runtime verification** was performed — no way to run the API and
  Angular dev servers together in this sandbox.

### Files changed this pass

```
frontend/bus-ticketing-client/src/app/features/auth/login/login.component.ts
frontend/bus-ticketing-client/src/app/features/auth/register/register.component.ts
frontend/bus-ticketing-client/src/assets/i18n/en.json
frontend/bus-ticketing-client/src/assets/i18n/bn.json
frontend/bus-ticketing-admin/src/app/features/auth/login/login.component.ts
frontend/bus-ticketing-admin/src/app/layout/shell.component.ts
frontend/bus-ticketing-admin/src/assets/i18n/en.json
frontend/bus-ticketing-admin/src/assets/i18n/bn.json
ai-handover.md
RELEASE_NOTES.md
```

---

## 2026-08-13 — Auth response fix + i18n asset pipeline

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

