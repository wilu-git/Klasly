# Spec: Milestone 14 — Hardening: role-access tests, boundary tests, deploy to a host the phone can reach

> Status: **Approved** (2026-11-11). Open questions 1–5 answered as recommended: add `Microsoft.AspNetCore.Mvc.Testing` 10.0.14; the phone reaches the PC over LAN / hotspot; build the failed-emails list; no `TimeProvider` refactor; no MVC HTTP tests.
> Plan: `tasks/plan-m14.md` + `tasks/todo-m14.md`.
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.5 "authorization is enforced in the Web API", §27.8 scan checks + risk rules, §27.10 item 4, §14 secrets) → `CLAUDE.md` (§3.3 status codes, §3.6 Android, §6 Milestone 14) → this spec.
> Milestone 13's spec is in git history (commit `2547f7e`) and in `docs/reports/2026-11-11-1150-milestone-13-device-check/`.

## What exists today

- **Tests (`tests/Klasly.Tests`, xUnit v3, 88 test methods on a LocalDB `Klasly_Tests` database built by the real migrations):**
  - Boundaries already covered at the **rule / service** level:
    - `RiskRulesTests`: exactly 20% → at-risk, 19.99% → not; exactly 75 → not at-risk, 74.99 → at-risk, 74.996 rounds to 75.
    - `GradeTests`: score 0 and exactly MaxScore are saved.
    - `AssessmentServiceTests`: weights may total exactly 100, not more.
  - Ownership already covered at the **service** level: another teacher's assessments (`AssessmentServiceTests`), a parent's unlinked child (`OverviewTests`).
  - **Not covered at all:**
    - No test goes through HTTP, so the JWT, the `[Authorize]` policies and the status codes are untested.
    - No scan tests: none of the 7 checks, the Present/Late cut-off or the 1-step QR grace.
    - No session ownership tests (open / close / QR / attendance list / correction on another teacher's class).
- **Web API authorization:**
  - Four policies (`AdminOnly`, `TeacherOnly`, `StudentOnly`, `ParentOnly`), set once per controller.
  - `AuthController.Login` is the only `[AllowAnonymous]` action. `api/auth/me` and `api/attendance/scan` use a plain `[Authorize]`. Scan checks the role itself so another role gets 401, as §27.8 asks.
  - There is no fallback policy. A new controller that forgets `[Authorize]` would be open to anyone.
- **Web MVC:** every area controller has `[Authorize(Roles = "...")]`. MVC holds no data and no rules (§27.5), so the API is the security boundary.
- **Reaching the API from a phone:**
  - The API listens on `http://localhost:5201` only (`launchSettings.json`, `http` profile).
  - Android reads `klasly.apiBaseUrl` from `local.properties` (default `http://10.0.2.2:5201/api/`). Cleartext is allowed in the debug build only.
  - Real phones work today only over USB (`adb reverse tcp:5201 tcp:5201`). That is not a host the phone can reach on its own.
- **Deferred to this milestone (M13 spec, Q5):** the admin "Failed emails" list. §27.8 says permanently failed emails are "shown on the admin dashboard". The Worker writes `Status = Failed` rows today, but nothing shows them.

## Assumptions (correct these before we build)

1. **Role-access tests go through real HTTP.** They use `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing` 10.0.14, a **new test-only package** (see Q1). It runs the real API in memory (real JWT, policies, services, EF Core) against the same `Klasly_Tests` database. The test project overrides the settings below; no secret is committed. The keys are fake, test-only values:
   - `ConnectionStrings:Klasly`
   - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
   - `Qr:SigningKey`
   - `Fcm:ServiceAccountPath` stays empty, so no push leaves the machine.
2. **Tokens come from the real login.** Each test logs in with a seeded account (`POST /api/auth/login`, password `Klasly@123`). It does not hand-build JWTs, so the login path is tested too.
3. **The endpoint list drives the role tests, not a hand-typed list.** The test reads every controller endpoint from the app's `EndpointDataSource`, fills route ids with real seeded ids, and checks:
   - **No token** → `401` on every endpoint except `POST /api/auth/login`. This catches a forgotten `[Authorize]` on any future endpoint.
   - **Wrong role** → `403` on every endpoint that has a role policy. Every role is tried against every policy that is not its own.
   - **Scan with a non-Student token** → `401` "Session expired – please log in" (§27.8 check 1).
   - **Right role** → never `401`/`403` from the policy. The body may still fail validation (`400`), which is fine here.
4. **Ownership tests (HTTP, hand-written, one per rule in §27.5 / §27.8):**
   - Teacher Rosa on Mark's class 2 → `403` on: list/create sessions, open, close, QR, attendance list, attendance correction, list/create assessments, grades.
   - Parent on a child who is not linked → `403` (one HTTP case; the service test already covers the detail).
   - Student scanning a class they're not enrolled in → `403` "Not enrolled in this class".
5. **Scan tests (service level, `AttendanceService` + `QrTokenService` against the test DB, real clock):** all 7 checks in order, each with its exact status and message:
   - bad signature → 400
   - QR 2 steps old → 400 "QR expired – rescan"
   - QR 1 step old → **accepted** (the grace step)
   - session not Open → 409
   - not enrolled → 403
   - other device → 403
   - second scan → 409
6. **Boundary tests added:**
   - **Late cut-off:** a scan 1 minute inside `StartTime + LateAfterMinutes` → Present; 1 minute after → Late. The services read `DateTime.UtcNow` directly, so an exact-to-the-second test would need a `TimeProvider` refactor (see Q4).
   - **Risk end to end through `RiskCalculator`**, with thresholds read from the seeded `SystemSettings` (the rule-only tests use constants):
     - 1 Absent of 5 Finalized sessions → `IsAtRisk = true`;
     - a weighted average of exactly 75.00 with good attendance → `IsAtRisk = false`.
   - Score of MaxScore + 0.01 → `400` (only `0` and `MaxScore` are tested today).
7. **Small hardening fix found by these tests:** add a **fallback policy** (`RequireAuthenticatedUser`) in `Program.cs`. That makes "authenticated" the default for every endpoint, and Login keeps its `[AllowAnonymous]`. It is one line and changes no current behavior. Any other gap the tests find gets fixed in the smallest way and listed in the report.
8. **"A host the phone can reach" = the team's PC on the same Wi-Fi (LAN).** This needs no cloud account and no new secret anywhere (see Q2):
   - A new `lan` launch profile in `Klasly.API` listens on `http://0.0.0.0:5201`, still `Development`, so user-secrets load.
   - A Windows Firewall inbound rule for TCP 5201 on Private networks. **The team runs this command themselves** in an admin PowerShell; I don't change security settings.
   - Android: `klasly.apiBaseUrl=http://<PC's Wi-Fi IP>:5201/api/` in `local.properties`, then `installDebug`. No app code changes; the debug build already allows cleartext.
   - For the demo, the PC joins the **phone's hotspot**. Campus Wi-Fi often blocks phone-to-PC traffic ("client isolation"). On a hotspot the PC's IP stays the same between demos.
   - MVC and the Worker keep running on the PC as today. Only the API has to be reachable from the phone (the teacher's QR page is on the PC's browser).
9. **Failed-emails list (if Q3 = yes):** read-only.
   - `GET /api/notifications/failed` (`AdminOnly`) returns the newest 100 `Channel = Email, Status = Failed` rows: created (UTC), recipient name + email, type, subject, attempts, last error.
   - MVC **Admin › Failed emails** shows them in a Bootstrap table, with Manila times and an empty state.
   - No retry button and no delete. Fixing a bad address is done in Accounts as today.

## Objective

Prove that the security and business rules hold when they are called the way real clients call them, and get the app working on a real phone without a USB cable.

### User stories
- As the **team**, we can run `dotnet test` and see every endpoint refuse a missing token and every wrong role, so a forgotten `[Authorize]` can't slip into the demo.
- As a **teacher**, I can't see or change another teacher's sessions, attendance or grades, even by typing ids into the URL.
- As a **student**, my scan gets the exact message §27.8 promises for every failure, and a scan right at the late cut-off is graded fairly.
- As a **student on a real phone**, I can log in and scan the teacher's QR over Wi-Fi with no cable plugged in.
- As an **admin**, I can see which parent emails failed for good and why *(if Q3 = yes)*.

## Tech stack

- No change to the apps: .NET 10, ASP.NET Core 10, EF Core 10, xUnit v3, Kotlin/Compose.
- New, test project only: `Microsoft.AspNetCore.Mvc.Testing` **10.0.14**, the same version as every other Microsoft package. Nothing in ASP.NET Core 10 itself runs the full pipeline in memory.

## Commands

```bash
dotnet build Klasly.slnx
dotnet test Klasly.slnx
dotnet list package
dotnet run --project src/Klasly.API --launch-profile lan
```

Firewall rule (the team runs this once, in PowerShell **as Administrator**):

```powershell
New-NetFirewallRule -DisplayName "Klasly API 5201" -Direction Inbound -Protocol TCP -LocalPort 5201 -Action Allow -Profile Private
```

Android (from `android/KlaslyApp`, after setting `klasly.apiBaseUrl` in `local.properties`):

```bash
./gradlew assembleDebug installDebug
```

## Project structure (files this milestone touches)

```
tests/Klasly.Tests/
  Klasly.Tests.csproj           ← + Microsoft.AspNetCore.Mvc.Testing 10.0.14
  ApiFactory.cs                 ← NEW: WebApplicationFactory with test settings + login helper
  RoleAccessTests.cs            ← NEW: endpoint sweep (no token → 401, wrong role → 403, scan → 401)
  OwnershipTests.cs             ← NEW: Rosa on Mark's class, unlinked child, not-enrolled scan
  ScanTests.cs                  ← NEW: the 7 checks, grace step, late cut-off
  RiskCalculatorTests.cs        ← + exactly-20% / exactly-75 through SystemSettings
  GradeTests.cs                 ← + MaxScore + 0.01 refused
src/Klasly.API/
  Program.cs                    ← fallback policy; `public partial class Program;` if the factory needs it
  Properties/launchSettings.json← + `lan` profile
(Q3 = yes)
  Controllers/NotificationsController.cs, Services/… , Core/DTOs/FailedEmailResponse.cs, repository method
  src/Klasly.Web/Areas/Admin/…  ← FailedEmails controller action + view + nav link
docs/reports/<…>/                ← per CLAUDE.md §5, incl. real-phone screenshots
```

## Code style

Same as the existing tests. Plain names that say the rule, one rule per test, seeded ids from `TestData`, and a comment only for the *why*:

```csharp
[Fact]
public async Task Teacher_cannot_open_another_teachers_session()
{
    var rosa = await api.LoginAsync("rosa…@klasly.test");       // owns class 1 only
    var res = await rosa.PostAsync($"api/sessions/{marksSessionId}/open", null, Ct);

    Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
}
```

## Testing strategy

- **Levels:**
  - HTTP (`WebApplicationFactory`) for policies, status codes and ownership;
  - service + test DB for scan rules and risk end to end;
  - pure rules stay as they are.
- **Isolation:** every test makes its own class/session with `TestData`, as today, so tests don't clash on shared rows. One assembly-wide `Klasly_Tests` database, reset at the start of each run.
- **Real phone check (manual, by the team with me guiding):** on the `lan` profile, with no USB cable:
  1. log in as a student;
  2. scan the QR that the teacher's MVC page shows;
  3. the teacher sees the scan in the live list;
  4. the Attendance and Grades tabs load;
  5. a parent logs in and sees the child overview.
  Screenshots go in the report.
- **Done means:** `dotnet test` all green, `dotnet build` no warnings added, `assembleDebug` green, and the real-phone check passes.

## Boundaries

- **Always:**
  - fake, test-only keys in tests;
  - the test DB guard stays (never touch the `Klasly` database);
  - the API's error messages are asserted word for word, because the apps show them to users;
  - run `dotnet test` after every change.
- **Ask first:**
  - adding `Microsoft.AspNetCore.Mvc.Testing` (Q1);
  - any production code change beyond the fallback policy and the `lan` profile;
  - a `TimeProvider` refactor (Q4);
  - any cloud host or tunnel account (Q2).
- **Never:**
  - commit secrets or the PC's IP in `local.properties` (it's already git-ignored);
  - change Windows Firewall or other system security settings myself;
  - turn CORS on;
  - weaken a check to make a test pass;
  - delete or skip a failing test.

## Success criteria

- [ ] Every API controller endpoint except Login returns `401` with no token. The test finds the endpoints by itself.
- [ ] Every role-policy endpoint returns `403` for each of the 3 wrong roles; scan returns `401` for non-Students.
- [ ] Teacher ownership: `403` on all session, attendance and assessment endpoints of another teacher's class. Parent unlinked child → `403`. Not-enrolled scan → `403`.
- [ ] Each of the 7 scan checks has a test with the exact §27.8 status and message. A 1-step-old QR is accepted and a 2-step-old one is refused.
- [ ] Late cut-off: 1 min inside → Present, 1 min after → Late.
- [ ] Through `RiskCalculator` + seeded settings: exactly 20% → at-risk; exactly 75.00 → not at-risk.
- [ ] MaxScore + 0.01 → `400`.
- [ ] A fallback policy is in place, and all current tests still pass.
- [ ] `dotnet test` green. `dotnet list package` shows no Microsoft package below 10.x.
- [ ] A real Android phone, with no USB cable, logs in and scans on the PC's LAN address. The teacher's live list shows the scan (screenshots in the report).
- [ ] *(Q3 = yes)* An admin sees failed emails with their last error; non-admins get `403`.

## Open questions (my recommendation first)

1. **Add `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 to the test project?** It is the standard way to test the real JWT + policy pipeline. The only way around it is checking attributes by reflection, which never sends a request, so it can't prove the status codes. **Recommend: yes.**
2. **Which "host the phone can reach"?**
   - **(a) LAN / phone hotspot (recommended):** free, no accounts, no secrets leave the PC. Works only while the PC is on, and only on the same network.
   - **(b) Tunnel** (Microsoft Dev Tunnels / Cloudflare / ngrok): gives an HTTPS URL that works on mobile data. Needs an account sign-in, and the API is then open to the internet.
   - **(c) Cloud host** (Azure App Service + Azure SQL, or a free ASP.NET host): a real always-on host. It needs accounts and secrets as environment variables, the Worker has to run somewhere too, and it's the most work.

   **Recommend (a).** It meets "a host the phone can reach" for the demo with zero new risk.
3. **Build the admin "Failed emails" list in this milestone?** M13 deferred it here, and §27.8 requires it. It's small: one read-only endpoint and one table. **Recommend: yes, as the last task.**
4. **Exact-second late cut-off?** Testing `ScannedAt == StartTime + LateAfterMinutes` exactly needs `TimeProvider` injected into `AttendanceService` (and `QrTokenService`). **Recommend: no.** ±1 minute proves the `≤` rule. Refactoring production code only for a test is not worth it this late.
5. **MVC role tests too?** MVC holds no data, and every area already has `[Authorize(Roles)]`, so the API is the boundary (§27.5). **Recommend: no MVC HTTP tests.** Only a one-line check in the review that every area controller keeps its attribute.
