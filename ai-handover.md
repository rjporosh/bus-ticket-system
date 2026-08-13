# AI Handover — Auth Response & Localization Fix Pass

**Date:** 2026-08-13
**Branch:** `main`
**Commits this pass:** `db6152f` … `8157674` (5 commits, see `git log db6152f^..8157674`)
**Source task:** fix Login/Register not returning tokens (Result pattern broken) + Angular
i18n assets 404'ing on both client and admin apps.

> Note: an older `ai-handover.md` about integration-test host-building issues previously
> lived at this path. That work is unrelated to this pass and is superseded by this file.
> If that work is still unresolved, check `git log -- ai-handover.md` for the prior version.

Read this top to bottom before touching the code. Section 1 is what's done and verified.
Section 2 is what's left, in priority order, with concrete pointers. Section 3 is the
exact prompt to hand to the next agent (or to run yourself).

---

## 1. Completed & verified this pass

### 1.1 Backend — Login/Register/Refresh returned 204 with no tokens (FIXED)

**Root cause:** `AuthController.cs` called:
```csharp
return result.ToApiResult(_localizer);
```
`result` is `Result<AuthResponseDto>`. Passing `_localizer` **positionally** made C#
overload resolution bind to the **non-generic** `ToApiResult(this Result, ...)` extension
(valid via `Result<T>` → `Result` covariance) instead of the generic
`ToApiResult<T>(this Result<T>, ...)` overload — the generic overload wasn't even a valid
candidate, since `IStringLocalizer` can't convert to `Func<T, IResult>`. The non-generic
overload returns `NoContent()` (204) and **discards the value**. That's why the client
never received `accessToken` / `refreshToken` / `user`, even though the MediatR handlers
and `Result<AuthResponseDto>` construction were already correct.

**Fix (commit `db6152f`):** changed `Login`, `Register`, `Refresh` in
`src/Presentation/BusTicketing.Api/Controllers/V1/AuthController.cs` to pass the localizer
as a **named argument**: `result.ToApiResult(localizer: _localizer);`. This resolves to the
generic overload → `200 OK` with the full `AuthResponseDto` body. `Logout` was left
untouched (it legitimately returns a valueless `Result`, 204 is correct there).

**Not independently verified:** could not run `dotnet build` in this sandbox — no .NET SDK
installed and no network access to nuget.org (egress allowlist only covers npm/PyPI/GitHub
domains). The fix is a two-token argument-order change with no new symbols, so the
compile-time risk is low, but **the next agent must run `dotnet build` and the auth
integration tests** (`tests/BusTicketing.IntegrationTests/AuthControllerTests.cs` already
covers Login) before calling this done.

### 1.2 Frontend — i18n assets 404 (FIXED, build-verified)

**Root cause:** `frontend/bus-ticketing-client/angular.json` and
`frontend/bus-ticketing-admin/angular.json` only copied `public/` into the build output
(`build` and `test` targets). `src/assets/i18n/en.json` and `bn.json` were never emitted,
so `GET /assets/i18n/en.json` / `bn.json` 404'd in both apps at runtime, leaving
`TranslateService` with no data and templates rendering raw keys.

**Fix (commit `28bce8c`):** added `{ "glob": "**/*", "input": "src/assets" }` to the assets
array in the `build` and `test` targets of both `angular.json` files, preserving existing
entries (`public/`, and the client's `ngsw-config.json` entry).

**Verified:**
- `npm install` succeeds cleanly for both `bus-ticketing-client` and `bus-ticketing-admin`.
- `npx ng build --configuration development` succeeds for **both** apps with zero
  compile errors.
- Confirmed `dist/*/browser/i18n/en.json` and `bn.json` are present in the output for
  both apps after the fix (they were absent before).
- Confirmed client `ngsw-config.json` already has an asset group matching `/assets/**`,
  so no service-worker config changes were needed and PWA behavior is unaffected.

**Could not verify in this sandbox:** production builds (`--configuration production`)
fail for **both** apps, but only at the **font-inlining** step —
`Inlining of fonts failed ... fonts.googleapis.com ... returned status code: 403`. This is
the sandbox's network egress allowlist blocking `fonts.googleapis.com` (only npm/PyPI/GitHub
domains are reachable here), **not a code defect**. The build gets past compilation and
into the optimization/inlining stage, which confirms the TypeScript/template code is sound.
**The next agent must run `ng build --configuration production` in an environment with
normal internet access** to get a true zero-error/zero-warning production build
confirmation.

### 1.3 Frontend — TranslatePipe reached into private internals (FIXED, build-verified)

**Root cause:** `TranslatePipe` in both apps read
`this.translateService['_translations']` via bracket-notation access to a private field,
and duplicated the key-resolution/interpolation logic that already exists in
`TranslateService.getSync()`.

**Fix (commit `8fe8f9e`):** both pipes now call `translateService.getSync(key,
interpolateParams)` directly. Template API unchanged: `{{ 'app.title' | translate }}` and
`{{ 'app.key' | translate:params }}` behave identically; unknown keys still fall back to
the key itself (that fallback lives in `getSync()`, untouched).

### 1.4 Frontend — silent translation-load failures (FIXED, build-verified)

**Fix (commit `afc7bdb`):** `TranslateService.loadLanguage()`'s `catchError()` now logs
`console.error('[TranslateService] Failed to load translation file for language "<lang>":', error)`
before falling back to `{}`. Behavior on failure is otherwise unchanged.

### 1.5 Translation key coverage (AUDITED, no gaps found)

Every key referenced via `| translate`, `getSync(...)`, or `.get(...)` in both apps'
`.ts`/`.html` files was diffed against `en.json`/`bn.json`. **All keys already exist in
both files for both apps** — no missing-key or en/bn-mismatch issues were found in the
existing codebase.

### 1.6 One new translation key added (PARTIAL — see item 2.1 below)

**Commit `8157674`:** added `app.loginSubtitle` to both
`frontend/bus-ticketing-client/src/assets/i18n/en.json` ("Sign in to view your bookings")
and `bn.json` ("আপনার বুকিং দেখতে সাইন ইন করুন"). **This key is not yet referenced by any
template** — it was added in preparation for item 2.1 and is currently unused dead data.
Either wire it up (2.1) or remove it if the next agent's audit of the login screen takes a
different shape.

---

## 2. Remaining work (in priority order)

### 2.1 Hardcoded strings in client login screen — NOT STARTED
`frontend/bus-ticketing-client/src/app/features/auth/login/login.component.ts` has a fully
inline template with **zero** `| translate` usage: title, subtitle, field labels,
validation messages, and the "Don't have an account? Register now" link are all hardcoded
English strings. All the needed keys already exist in `en.json`/`bn.json` — confirmed
during this pass: `app.loginTitle`, `app.loginSubtitle` (just added), `app.username`,
`app.password`, `app.usernameRequired`, `app.passwordRequired`, `app.invalidCredentials`,
`app.dontHaveAccount`, `app.register`, `app.signIn`. Wire the template to use `| translate`
for every user-facing string, matching the existing `{{ 'app.key' | translate }}` pattern
used elsewhere in the app. Do not touch validation logic, routing, or the auth API calls —
this is template-only.

### 2.2 Hardcoded strings in client register screen — NOT AUDITED
`frontend/bus-ticketing-client/src/app/features/auth/register/register.component.ts` was
located but not yet inspected in this pass. Apply the same audit-and-translate approach as
2.1. Check for any keys used that aren't yet in `en.json`/`bn.json` (none were found missing
across the whole app in the 1.5 audit, but re-verify after editing in case new strings are
introduced).

### 2.3 Hardcoded strings in admin login screen — NOT AUDITED
`frontend/bus-ticketing-admin/src/app/features/auth/login/login.component.ts` (181 lines)
was located but not opened/audited in this pass. Same treatment as 2.1, using the admin
app's own `en.json`/`bn.json` (which already have `login`/`register` family keys per the
audit in 1.5).

### 2.4 Shared shell/layout + language switcher — NOT AUDITED
Task spec item 5 explicitly calls out "shared shell/layout" and "language switcher" as
in-scope for the hardcoded-string sweep. Not yet located or inspected this pass. Search for
component files under something like `core/layout`, `shared/shell`, or similar in both
`bus-ticketing-client` and `bus-ticketing-admin`, and audit their templates the same way.

### 2.5 Common buttons/messages — NOT AUDITED
Task spec item 5 also calls out generic shared buttons/messages (e.g. Save/Cancel/Submit,
generic success/error toasts). Not yet swept. Grep both apps' `.html` files for
English string literals outside `| translate` pipes as a starting point — e.g.:
```bash
grep -rn '>[A-Z][a-z]' src/app --include="*.html" | grep -v "translate"
```
(This is a rough heuristic, not exhaustive — manually verify each hit.)

### 2.6 Full build verification — PARTIALLY DONE, NEEDS A NORMAL-NETWORK ENVIRONMENT
- Done: `npm install` — both apps, clean.
- Done: `ng build --configuration development` — both apps, zero errors.
- Blocked in this sandbox only: `ng build --configuration production` — blocked by
  `fonts.googleapis.com` being outside the network egress allowlist (403 on font
  inlining, not a code issue). Re-run in an environment with normal internet access and
  confirm literally zero errors and zero warnings, per the original task's requirement.
- Not run at all: `dotnet build` for the full .NET solution — no .NET SDK / no
  nuget.org access in this sandbox. Must be run fresh.
- Not run: `dotnet test`. `tests/BusTicketing.IntegrationTests/AuthControllerTests.cs`
  and `tests/BusTicketing.UnitTests/Application/ResultTests.cs` are the most relevant
  existing suites for the auth fix; run the full suite regardless.

### 2.7 Runtime/manual verification — NOT DONE
None of the following from the original task's verification checklist has been exercised
against a running instance (no way to run the .NET API + Angular dev servers together in
this sandbox):
- Login returns 200 + full `AuthResponseDto` body (curl/Postman against a running API).
- Register returns 200 + full `AuthResponseDto` body.
- `/assets/i18n/en.json` and `bn.json` return 200 from **both** running dev servers (client
  and admin) — the build-output check (1.2) is a strong proxy but isn't the same as an
  actual HTTP 200 from `ng serve`.
- Switching EN → BN and back changes visible UI text in the browser.
- Successful login/register stores `accessToken`/`refreshToken` in the browser (localStorage
  or wherever `AuthService` persists them — confirmed the service consumes the response
  correctly by reading the code, but not exercised at runtime).
- No regression in booking, route, bus, payment, notification, or refresh-token flows —
  only spot-checked by reading `RefreshToken.cs`/`AuthDtos.cs`; not run end-to-end.

---

## 3. Next-agent command

Paste this to the next agent (or use it yourself) to continue from exactly where this pass
left off:

```
Continue the BusTicketingSystem auth/i18n fix pass documented in ai-handover.md at the
repo root. Read ai-handover.md section 2 in full before making any changes — it lists
completed work (do not redo it) and the exact remaining items in priority order.

Do ONLY the following, in this order, and nothing else:

1. Wire up frontend/bus-ticketing-client/src/app/features/auth/login/login.component.ts's
   inline template to use the `| translate` pipe for every user-facing string (title,
   subtitle, labels, validation/error messages, the register link). All required keys
   already exist in en.json/bn.json, including app.loginSubtitle which was added but not
   yet used - confirm it fits before using it, or replace it if it doesn't match the
   actual template text you write.
2. Audit and fix the same way:
   - frontend/bus-ticketing-client/src/app/features/auth/register/register.component.ts
   - frontend/bus-ticketing-admin/src/app/features/auth/login/login.component.ts
3. Locate and audit the shared shell/layout component(s) and language switcher in both
   bus-ticketing-client and bus-ticketing-admin for hardcoded user-facing strings, and fix
   them the same way.
4. Sweep both apps for remaining hardcoded common buttons/messages (Save/Cancel/Submit,
   generic success/error toasts) outside auth screens. Do NOT translate code identifiers,
   CSS classes, routes, API paths, enum values, logging text, or developer comments.
5. For every new string you translate, confirm the key exists in both en.json and bn.json
   for that app; add any missing key/value pairs to BOTH files together in the same commit,
   with real Bengali translations (not machine-transliterated placeholders).
6. Run `npm install` and `ng build --configuration production` for both Angular apps and
   confirm ZERO build errors and ZERO warnings. (Note: the author of db6152f/28bce8c could
   not verify production builds in their sandbox due to fonts.googleapis.com being network-
   blocked there - if you hit the same issue, note it explicitly rather than silently
   switching to a development build.)
7. Run `dotnet build` on the full solution and confirm ZERO errors and ZERO warnings -
   this has not been run at all yet against the auth fix in db6152f.
8. Run `dotnet test` (or at minimum
   tests/BusTicketing.IntegrationTests/AuthControllerTests.cs and
   tests/BusTicketing.UnitTests/Application/ResultTests.cs) and confirm they pass.
9. If you have a way to run the API and both Angular dev servers, manually verify: Login
   and Register return 200 with a full AuthResponseDto body; /assets/i18n/en.json and
   bn.json return 200 in both apps; switching EN<->BN updates visible UI text; tokens are
   persisted after login/register; no booking/route/bus/payment/notification/refresh-token
   regression.
10. Update ai-handover.md: move completed items from section 2 into section 1 with the
    same level of detail (root cause if applicable, what was verified and how, what
    still couldn't be verified and why), and update section 2/3 to reflect what's left
    after your pass, so a third agent could pick this up cleanly.

Use the same commit discipline as db6152f..8157674 in this repo's git log: one logical
fix per commit, Conventional Commits format, a body explaining root cause and the fix -
not just "fix translations". Do not redesign, remove, or regress any unrelated feature.
Do not introduce a new i18n library. Do not modify the database schema.
```
