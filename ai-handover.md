# AI Handover — Auth Response & Localization Fix Pass

**Date:** 2026-08-18
**Branch:** `main`
**Commits this pass:** `e2268d0` … `6708b6a` (4 commits on top of the previous pass's
`db6152f..8157674`, see `git log db6152f^..6708b6a`)
**Source task:** continue the i18n hardcoded-string sweep left in section 2 of the
previous version of this file (auth screens, shared shell, common buttons).

Read this top to bottom before touching the code. Section 1 is what's done and verified.
Section 2 is what's left, in priority order, with concrete pointers. Section 3 is the
exact prompt to hand to the next agent (or to run yourself).

---

## 1. Completed & verified

### 1.1 Backend — Login/Register/Refresh returned 204 with no tokens (FIXED)

**Root cause:** `AuthController.cs` called `result.ToApiResult(_localizer)` and the
positional `_localizer` argument made C# overload resolution silently bind to the
non-generic `ToApiResult(this Result, ...)` overload instead of the generic
`ToApiResult<T>` one, discarding the `AuthResponseDto` value and returning 204.

**Fix (commit `db6152f`):** changed `Login`, `Register`, `Refresh` in
`src/Presentation/BusTicketing.Api/Controllers/V1/AuthController.cs` to pass
`result.ToApiResult(localizer: _localizer);` as a named argument. `Logout` was left
untouched (204 is correct there).

**Still not independently verified — see 2.2 below.** No .NET SDK and no reachable
NuGet source in either this sandbox or the previous one. The fix is a two-token
argument-order change with no new symbols, so compile-time risk is low, but it has
**never actually been built or test-run** against real MSBuild/xUnit in any pass so far.

### 1.2 Frontend — i18n assets 404 (FIXED, build-verified)

**Root cause:** `angular.json` for both apps only copied `public/` into build output;
`src/assets/i18n/*.json` were never emitted, so `GET /assets/i18n/en.json`/`bn.json`
404'd at runtime.

**Fix (commit `28bce8c`):** added `{ "glob": "**/*", "input": "src/assets" }` to the
assets array in the `build`/`test` targets of both `angular.json` files.

**Verified again this pass:** `npx ng build --configuration development` succeeds for
both apps with zero errors, and `dist/*/browser/i18n/en.json` + `bn.json` are present
with all newly-added keys (see 1.5–1.8 below) intact in the built output.

### 1.3 Frontend — TranslatePipe reached into private internals (FIXED, build-verified)

`TranslatePipe` in both apps calls `translateService.getSync(key, interpolateParams)`
instead of bracket-accessing a private field. Template API unchanged. (Commit `8fe8f9e`.)

### 1.4 Frontend — silent translation-load failures (FIXED, build-verified)

`TranslateService.loadLanguage()` now logs a `console.error` on failed fetch before
falling back to `{}`. (Commit `afc7bdb`.)

### 1.5 Client login screen — hardcoded strings (FIXED, build-verified)

**Commit `e2268d0`.** `LoginComponent`'s inline template had zero `| translate` usage.
Wired title, subtitle, both field labels, both validation messages, the submit button,
and the register-link footer to existing keys (`app.loginTitle`, `app.loginSubtitle`,
`app.usernameRequired`, `app.password`, `app.passwordRequired`, `app.signIn`,
`app.dontHaveAccount`, `app.registerNow`). The API-error fallback string lives in the
`.subscribe({ error })` TS callback, not the template, so it can't use the pipe —
injected `TranslateService` and used `getSync('app.invalidCredentials')` there instead,
mirroring the pattern already used by `LanguageSwitcherComponent`.

**New key added** (missing from both locales, added together in the same commit):
`app.usernameOrEmail` — "Username or Email" / "ব্যবহারকারী নাম বা ইমেইল". The field's
actual label is broader than the existing `app.username` ("Username"), so reusing that
key would have silently narrowed the label's meaning.

`app.loginSubtitle` (added dead/unused in `8157674`) matched this screen's actual
subtitle text verbatim and was wired up as-is — no changes needed.

### 1.6 Client register screen — hardcoded strings (FIXED, build-verified)

**Commit `92cb15d`.** Same treatment as 1.5: all template strings wired to
`| translate`; the `Swal.fire(...)` success dialog and the API-error fallback (both TS
code) use `TranslateService.getSync(...)`.

**Data bug found and fixed:** `app.passwordMinLength` said "at least 6 characters" in
both `en.json`/`bn.json`, but this screen's validator (and the admin user-creation
form's validator) actually enforces `Validators.minLength(8)`. This key had **zero
usages anywhere in the codebase** before this commit — confirmed via
`grep -rn "passwordMinLength"` — so it was safe to correct rather than introduce a
second, differently-named key. Both locale values now say "8 characters".

**New keys added** (both locales, real Bengali, not transliteration):
`app.registerSubtitle`, `app.usernameMinLength`, `app.emailRequired` (distinct from the
pre-existing `app.emailInvalid`, which only covers the format-invalid case),
`app.accountCreated`, `app.accountCreatedMessage`, `app.registrationFailed`.

### 1.7 Admin login screen — hardcoded fallback (FIXED, build-verified)

**Commit `a7326d7`.** The admin login template was *already* fully translated from an
earlier pass (title, subtitle, labels, validation errors, submit button, example-users
hint all used `| translate`). The one gap was the TS-side fallback error string
`'Invalid username or password.'` in the `.subscribe({ error })` handler — replaced with
`this.translate.getSync('app.invalidCredentials')`. No new key needed; it already
existed in `frontend/bus-ticketing-admin/src/assets/i18n/{en,bn}.json`.

### 1.8 Shared shell/layout — audited both apps (FIXED where needed, build-verified)

- **Client `ShellComponent`:** already fully translated (brand, nav links, online/offline
  status, logout/login/register buttons, footer copyright). No changes needed.
- **Client & admin `LanguageSwitcherComponent`:** both correctly show only the literal
  language codes `EN` / `BN` — these are not translatable prose, left as-is.
- **Admin `ShellComponent`:** had four hardcoded strings — sidebar brand title
  ("Bus Ticketing"), brand subtitle ("Dispatch Console"), the "Sign out" menu item, and
  the "Account menu" aria-label on the user-menu trigger button. Fixed in commit
  `6708b6a`. The aria-label uses `[attr.aria-label]="'app.accountMenu' | translate"`
  since Angular attribute bindings need that form rather than the pipe inline in a
  static attribute.

  **New keys added** (both locales): `app.brandTitle` ("Bus Ticketing" / "বাস টিকেটিং" —
  kept deliberately separate from the existing `app.title`, "Bus Ticketing System", so
  the sidebar's shorter text isn't silently changed by reuse), `app.dispatchConsole`,
  `app.accountMenu`, `app.signOut` (kept distinct from the pre-existing `app.logout`,
  "Logout", which is a different label already used elsewhere in the admin app).

  **Not translated:** the "v1.0.0 · MVP" build-version stamp in the sidebar footer — a
  version tag, not natural-language UI copy, per the task's instruction not to translate
  code-like identifiers.

### 1.9 Translation key coverage for everything touched this pass (AUDITED)

Every key referenced via `| translate` or `getSync(...)` in the files touched this pass
(both login screens, both register/shell files above) was confirmed present in **both**
`en.json` and `bn.json` for its app, with real (non-transliterated) Bengali text. No
en/bn mismatches were introduced.

### 1.10 Build verification this pass

- `npm install` — clean, zero errors, both apps (re-verified fresh in this sandbox).
- `npx ng build --configuration development` — zero errors, both apps. Confirmed
  `dist/*/browser/i18n/en.json` and `bn.json` are present and contain every new key
  added in 1.5–1.8.
- `npx ng build --configuration production` — **still blocked in this sandbox** by the
  exact same issue as the previous pass: font inlining fails on
  `fonts.googleapis.com` returning `403` (`x-deny-reason: host_not_allowed` from the
  sandbox's egress proxy — confirmed via direct `curl`), for **both** apps. This is the
  sandbox's network allowlist, not a code defect — the build gets past full compilation
  and into the optimization/font-inlining stage for both apps before failing, which is
  the same signal as before. **A real zero-error/zero-warning production build has
  still never been obtained in any sandbox pass.** The next agent must run this with
  normal internet access.

---

## 2. Remaining work (in priority order)

### 2.1 Common-buttons/messages sweep — AUDITED, SCOPE IS MUCH LARGER THAN EXPECTED, NOT STARTED

The original task framed this as a "sweep for common buttons/messages
(Save/Cancel/Submit, generic success/error toasts) outside auth screens." The audit
found something bigger: entire **non-auth feature screens** have zero `| translate`
usage at all — not just a few stray buttons. Confirmed via
`grep -L TranslatePipe` across every `*.component.ts`:

- **Client:** `features/booking/booking.component.ts` (535 lines),
  `features/search/search.component.ts` (157 lines),
  `features/my-tickets/my-tickets.component.ts` (212 lines) — headers, field labels,
  buttons, empty-states, everything is hardcoded English. (`app.component.ts` and the
  language switcher are correctly excluded — see 1.8.)
- **Admin:** `features/schedules/schedules.component.ts`,
  `features/users/users.component.ts`, `features/booking/booking.component.ts`,
  `features/stations/stations.component.ts`, `features/buses/buses.component.ts`,
  `features/roles/roles.component.ts`, `features/routes-mgmt/routes.component.ts` — same
  situation, and these are large CRUD screens with dialogs, tables, and forms (e.g.
  `buses.component.ts` alone has zero i18n from its `<h1>Fleet</h1>` header down).

A concrete, lower-risk starting point that **is** true "common buttons": generic
Save/Cancel/Close/Back buttons already found via
`grep -rnE ">(Save|Cancel|Submit|Delete|Edit|Close|Confirm|Yes|No|Back|Next)<" --include="*.ts" | grep -v translate`:
- Admin dialog footers in `schedules`, `users`, `stations`, `buses`, `roles`,
  `routes-mgmt` — all use the identical pattern
  `<button mat-button type="button" mat-dialog-close>Cancel</button>` /
  `<button mat-flat-button ... type="submit" ...>Save</button>`, and the keys
  `app.cancel`/`app.save` already exist in admin's `en.json`/`bn.json`.
- `admin/features/buses/buses.component.ts:525` — a lone `Close` dialog button
  (`app.close` exists).
- `admin/features/booking/booking.component.ts:260` — a `Back` step button
  (`app.back` exists).
- `client/features/my-tickets/my-tickets.component.ts:89` — a `Close` QR-dialog button
  (`app.close` exists in client's `en.json`/`bn.json` too).

None of these 8 files currently import `TranslatePipe` at all, so fixing even just the
buttons above means adding the import + wiring 1-2 lines per file — low risk, high
value, and matches the task's literal "common buttons" framing without taking on full
per-screen translation (headers, table columns, every form label) of large,
untested-in-this-sandbox CRUD screens in the same pass.

**Recommendation for the next agent:** do the low-risk common-button pass above first
(one commit, or one per file if you want tighter diffs), then treat "fully translate
booking/search/my-tickets (client) and schedules/users/booking/stations/buses/roles/
routes-mgmt (admin)" as its own follow-up handover item — it's realistically several
sessions of work given the number of screens and the volume of strings in each, and
rushing it without `ng build` access to a non-font-blocked environment is risky.

### 2.2 Full build verification — STILL NOT DONE, NEEDS A NORMAL-NETWORK / SDK ENVIRONMENT

- Done again this pass: `npm install` — both apps, clean.
- Done again this pass: `ng build --configuration development` — both apps, zero errors.
- Still blocked in every sandbox so far: `ng build --configuration production` — blocked
  by `fonts.googleapis.com` returning 403 (egress-proxy `host_not_allowed`), not a code
  issue. Re-run with normal internet access and confirm literally zero errors/warnings.
- Still not run at all, in any pass: `dotnet build` for the full solution. This sandbox
  has no `dotnet` binary preinstalled; `apt-get install dotnet-sdk-8.0` was attempted and
  failed — `security.ubuntu.com` returned 404 for the exact `.deb` files apt tried to
  fetch (mirror/version mismatch, not just a blocked domain), and `api.nuget.org` is
  blocked outright by the egress allowlist (`host_not_allowed`, confirmed via `curl`).
  **This must be run fresh in an environment with a working .NET SDK before the auth
  fix in `db6152f` can be considered verified**, and none of the three passes on this
  repo so far (this one included) have been able to do it.
- Still not run: `dotnet test`, for the same reason.

### 2.3 Runtime/manual verification — STILL NOT DONE

Unchanged from the previous handover — no way to run the .NET API + Angular dev servers
together in this sandbox (no dotnet). None of the following has been exercised:
- Login/Register return 200 + full `AuthResponseDto` body.
- `/assets/i18n/en.json`/`bn.json` return 200 from **running** dev servers (the
  build-output check in 1.10 is a strong proxy but not the same as an HTTP 200 from
  `ng serve`).
- Switching EN → BN and back changes visible UI text in a real browser, including the
  newly-wired strings from this pass.
- Tokens persist in the browser after login/register.
- No regression in booking, route, bus, payment, notification, or refresh-token flows.

---

## 3. Next-agent command

```
Continue the BusTicketingSystem auth/i18n fix pass documented in ai-handover.md at the
repo root. Read ai-handover.md section 2 in full before making any changes — it lists
completed work (do not redo it) and the exact remaining items in priority order.

Do ONLY the following, in this order, and nothing else:

1. Common-buttons pass (ai-handover.md item 2.1): in the 8 files listed there (client
   my-tickets.component.ts; admin schedules/users/booking/stations/buses/roles/
   routes-mgmt.component.ts), import TranslatePipe and wire ONLY the generic
   Save/Cancel/Close/Back dialog buttons already identified to the existing app.save /
   app.cancel / app.close / app.back keys. Do NOT touch any other string in these files
   (headers, table columns, form labels, toasts) in this step — that is out of scope
   here per item 2.1's recommendation.
2. If time and scope allow after step 1, treat full translation of the remaining
   hardcoded strings in those same 8 files (client booking/search/my-tickets; admin
   schedules/users/booking/stations/buses/roles/routes-mgmt) as its own tracked
   sub-effort: pick ONE screen, audit it fully against en.json/bn.json (add any missing
   keys to both files together, real Bengali translations), wire it, verify with
   `ng build --configuration development`, commit, and stop — do not attempt all
   remaining screens in one pass. Update ai-handover.md with exactly which screen(s) got
   done and which are still pending.
3. Run `npm install` and `ng build --configuration production` for both Angular apps.
   If you hit the same fonts.googleapis.com 403 as every prior pass, note it explicitly
   (do not silently fall back to a development build as "done").
4. Run `dotnet build` on the full solution and `dotnet test` (or at minimum
   tests/BusTicketing.IntegrationTests/AuthControllerTests.cs and
   tests/BusTicketing.UnitTests/Application/ResultTests.cs). This has not succeeded in
   any pass so far due to missing SDK / blocked NuGet access — if your environment has
   real internet access and a .NET SDK, this is the single highest-value thing you can
   verify, since the auth fix in db6152f has never been build-checked.
5. If you have a way to run the API and both Angular dev servers, manually verify:
   Login/Register return 200 with a full AuthResponseDto body; /assets/i18n/en.json and
   bn.json return 200 in both apps; switching EN<->BN updates visible UI text including
   strings added in this pass; tokens persist after login/register; no
   booking/route/bus/payment/notification/refresh-token regression.
6. Update ai-handover.md: move completed items from section 2 into section 1 with the
   same level of detail (root cause if applicable, what was verified and how, what
   still couldn't be verified and why), and update section 2/3 to reflect what's left
   after your pass, so a fourth agent could pick this up cleanly.

Use the same commit discipline as this repo's git log: one logical fix per commit,
Conventional Commits format, a body explaining root cause and the fix - not just "fix
translations". Do not redesign, remove, or regress any unrelated feature. Do not
introduce a new i18n library. Do not modify the database schema.
```
