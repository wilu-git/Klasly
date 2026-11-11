# Milestone 14 — Tasks (Hardening: role-access tests, boundary tests, phone over Wi-Fi)

> Plan: [`plan-m14.md`](./plan-m14.md) · Spec: [`SPEC.md`](../SPEC.md).
> After every .NET task: `dotnet build Klasly.slnx` and `dotnet test Klasly.slnx` (LocalDB must be running). After package changes: `dotnet list package`.
> After every task: a report folder in `docs/reports/` + a line in `docs/reports/INDEX.md` (CLAUDE.md §5), and `graphify update .`.

---

## Task 1: API reachable from a real phone over Wi-Fi

**Description:** Add a `lan` launch profile so the API listens on every network card, and walk the team through the firewall rule and the real-phone check. The phone then reaches the API with no USB cable.

**Acceptance criteria:**
- [x] `src/Klasly.API/Properties/launchSettings.json` has a `lan` profile: `http://0.0.0.0:5201`, `ASPNETCORE_ENVIRONMENT=Development`, `launchBrowser: false`.
- [ ] The team has added the firewall rule themselves (admin PowerShell, `-Profile Private`). Claude doesn't run it.
- [ ] A real phone on the same Wi-Fi or hotspot, built with `klasly.apiBaseUrl=http://<PC IP>:5201/api/`, logs in as a student with no cable plugged in.

**Verification:**
- [x] `dotnet run --project src/Klasly.API --launch-profile lan` logs `Now listening on: http://0.0.0.0:5201`.
- [ ] From the phone's browser, `http://<PC IP>:5201/api/auth/me` returns the 401 problem-details JSON. That proves the network path before the app is involved.
- [ ] Manual: the student logs in on the app; screenshot in the report.

**Dependencies:** None (needs the team, a phone and the PC on the same network).

**Files likely touched:**
- `src/Klasly.API/Properties/launchSettings.json`
- `android/KlaslyApp/local.properties` (team's machine only, git-ignored)

**Estimated scope:** XS

---

## Task 2: In-memory API test host

**Description:** Add `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 to the test project and an `ApiFactory` that runs the real API against `Klasly_Tests` with fake keys. A `LoginAsync(email)` helper returns an authenticated `HttpClient`. One smoke test proves the whole thing starts.

**Acceptance criteria:**
- [x] `ApiFactory` sets these through `UseSetting`, with environment `Testing`:
  - `ConnectionStrings:Klasly` → the `Klasly_Tests` connection string from `DatabaseFixture`;
  - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` and `Qr:SigningKey` (fake, test-only);
  - an empty `Fcm:ServiceAccountPath`.
- [x] `LoginAsync("admin@klasly.test")` returns a client whose `GET api/auth/me` is `200` with role `Admin`. Clients are cached per email.
- [x] `Program.cs` ends with `public partial class Program;` only if the factory needs it; no other production change.

**Verification:**
- [x] `dotnet test --filter ApiFactory` green; full `dotnet test` green.
- [x] `dotnet list package` shows `Microsoft.AspNetCore.Mvc.Testing 10.0.14` and no Microsoft package below 10.x.

**Dependencies:** None

**Files likely touched:**
- `tests/Klasly.Tests/Klasly.Tests.csproj`
- `tests/Klasly.Tests/ApiFactory.cs` (new)
- `tests/Klasly.Tests/ApiFactoryTests.cs` (new, smoke test)
- `src/Klasly.API/Program.cs` (one line, if needed)

**Estimated scope:** S

---

### Checkpoint A
- [x] `dotnet build` and `dotnet test` green.
- [ ] Real phone logs in over Wi-Fi (Task 1).
- [ ] Review with the team before Phase 2.

---

## Task 3: Fallback policy and endpoint sweep

**Description:** Make "logged in" the default for every API endpoint, and add one test class that finds every controller endpoint by itself and checks the role rules for each.

**Acceptance criteria:**
- [x] `Program.cs`: `.SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())` on the existing `AddAuthorizationBuilder()`.
- [x] `RoleAccessTests` reads the endpoints from `EndpointDataSource` (controller actions only) and asserts:
  - **no token** → `401` on every endpoint except `POST api/auth/login`;
  - **every wrong role** → `403` on every `*Only` policy endpoint (each of the 3 roles that don't match);
  - **scan** (`POST api/attendance/scan`) with an Admin, Teacher or Parent token → `401` with detail "Session expired – please log in";
  - **right role** → never `401` / `403` from the policy.
- [x] The test fails with the endpoint's route in the message, so a broken endpoint is easy to find. A sanity check makes sure the endpoint count is above 40, so an empty list can't pass by accident.

**Verification:**
- [x] `dotnet test --filter RoleAccessTests` green; full `dotnet test` green.
- [x] Manual check: remove `[Authorize]` from one controller locally → the test goes red and names it → put it back.

**Dependencies:** Task 2

**Files likely touched:**
- `src/Klasly.API/Program.cs`
- `tests/Klasly.Tests/RoleAccessTests.cs` (new)

**Estimated scope:** S

---

## Task 4: Ownership tests over HTTP

**Description:** Prove over HTTP that a user with the right role still can't reach someone else's data (§27.5).

**Acceptance criteria:**
- [x] Rosa (teacher 1) gets `403` on Mark's class 2 and on a session of class 2, for each of:
  - `GET` / `POST api/classes/2/sessions`;
  - `POST api/sessions/{id}/open`, `POST …/close`;
  - `GET …/qr`, `GET …/attendance`, `PUT …/attendance/{studentId}`;
  - `GET` / `POST api/classes/2/assessments`;
  - `GET` / `PUT api/assessments/{id}/grades` for an assessment of class 2.
- [x] Parent Linda gets `403` "You can only view your own children" on student 1's overview.
- [x] A student who isn't enrolled, scanning a valid QR of an open session, gets `403` "Not enrolled in this class".

**Verification:**
- [x] `dotnet test --filter OwnershipTests` green; full `dotnet test` green.

**Dependencies:** Task 2

**Files likely touched:**
- `tests/Klasly.Tests/OwnershipTests.cs` (new)
- `tests/Klasly.Tests/TestData.cs` (a helper to make a session / assessment on a given class, if needed)

**Estimated scope:** S

---

## Task 5: Scan tests

**Description:** Test the 7 scan checks of §27.8 in order, with exact statuses and messages, plus the 1-step grace and the late cut-off.

**Acceptance criteria:**
- [x] One test per check, each passing every earlier check:
  - bad signature → `400` "Invalid QR code";
  - step −2 → `400` "QR expired – rescan";
  - session not Open → `409` "Session is closed";
  - not enrolled → `403` "Not enrolled in this class";
  - a different device id → `403` "Unregistered device";
  - second scan → `409` "Already checked in".
  (Check 1, the role, is covered by Task 3.)
- [x] A step −1 token is accepted (the grace step).
- [x] `StartTime = now − LateAfterMinutes + 1 min` → Present; `StartTime = now − LateAfterMinutes − 1 min` → Late. Each test sets the record's `Source = Scan` and `ScannedAt`.

**Verification:**
- [x] `dotnet test --filter ScanTests` green, 3 runs in a row (no flakiness at step boundaries); full `dotnet test` green.

**Dependencies:** None

**Files likely touched:**
- `tests/Klasly.Tests/ScanTests.cs` (new)
- `tests/Klasly.Tests/TestData.cs` (session helper, shared with Task 4)

**Estimated scope:** S

---

## Task 6: Boundary additions

**Description:** Run the 20% / 75 boundaries through the real `RiskCalculator` with thresholds read from the seeded `SystemSettings`, and test the score just over MaxScore.

**Acceptance criteria:**
- [x] `RiskCalculatorTests`: an enrollment with 1 Absent out of 5 Finalized sessions → latest snapshot `AbsenceRate = 0.2000`, `IsAtRisk = true`.
- [x] `RiskCalculatorTests`: good attendance and a weighted average of exactly 75.00 → `IsAtRisk = false`.
- [x] `GradeTests`: score `MaxScore + 0.01` → refused with the field error. Nothing is saved.

**Verification:**
- [x] `dotnet test --filter "RiskCalculatorTests|GradeTests"` green; full `dotnet test` green.

**Dependencies:** None

**Files likely touched:**
- `tests/Klasly.Tests/RiskCalculatorTests.cs`
- `tests/Klasly.Tests/GradeTests.cs`

**Estimated scope:** S

---

### Checkpoint B
- [x] `dotnet test` green.
- [x] Every role, ownership, scan and boundary criterion in the spec is ticked.
- [x] Any production gap a test found is fixed in the smallest way and listed in the task's report.
- [ ] Review with the team before Phase 4.

---

## Task 7: Failed-emails API

**Description:** A read-only admin endpoint listing emails that failed for good (§27.8), newest first.

**Acceptance criteria:**
- [x] `GET /api/notifications/failed` (`AdminOnly`) returns at most 100 `Channel = Email, Status = Failed` rows, newest first, as `FailedEmailResponse`:
  - `NotificationId`, `CreatedAt` (UTC);
  - `RecipientName`, `RecipientEmail`;
  - `Type`, `Subject`, `Attempts`, `LastError`.
- [x] The query is a projection in `NotificationRepository` (no entities leave the API). Pending, Accepted and Push rows never appear.
- [x] The endpoint sweep (Task 3) automatically covers its `401` / `403`.

**Verification:**
- [x] A new test (`FailedEmailsTests`) seeds one Failed email, one Pending email and one Failed push. Only the Failed email is returned, with its last error.
- [x] `dotnet test` green.

**Dependencies:** Task 2 (for the HTTP sweep coverage)

**Files likely touched:**
- `src/Klasly.Core/DTOs/FailedEmailResponse.cs` (new)
- `src/Klasly.Core/Interfaces/INotificationRepository.cs` + `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs`
- `src/Klasly.API/Controllers/NotificationsController.cs` (new). It calls the repository through a small service, following the existing layer rule.
- `tests/Klasly.Tests/FailedEmailsTests.cs` (new)

**Estimated scope:** M

---

## Task 8: MVC Admin › Failed emails page

**Description:** Show the failed emails to the admin in the web portal, read-only.

**Acceptance criteria:**
- [x] The Admin nav has a "Failed emails" item. `Admin/FailedEmails/Index` shows a Bootstrap table with:
  - created (Manila time), recipient, type, subject, attempts, last error;
  - an empty state: "No failed emails. Every alert reached Brevo."
- [x] Data comes through an API-backed repository (typed `HttpClient` + `BearerTokenHandler`). The API's error messages show through `_Alerts`.
- [ ] `/impeccable` pass on the view.

**Verification:**
- [x] `dotnet build` green.
- [ ] Manual in the browser pane: as admin, the page lists a seeded Failed row; as a teacher, the URL is refused.

**Dependencies:** Task 7

**Files likely touched:**
- `src/Klasly.Web/Areas/Admin/Controllers/FailedEmailsController.cs` (new)
- `src/Klasly.Web/Areas/Admin/Views/FailedEmails/Index.cshtml` (new)
- `src/Klasly.Web/Repositories/NotificationRepository.cs` + interface (new)
- `src/Klasly.Web/Program.cs` (register the client), `src/Klasly.Web/Views/Shared/_Layout.cshtml` (nav item)

**Estimated scope:** M

---

### Checkpoint C (complete)
- [x] All spec success criteria ticked in `SPEC.md`.
- [ ] `dotnet build`, `dotnet test`, `dotnet list package` and `./gradlew assembleDebug` all clean.
- [ ] Final real-phone run over Wi-Fi, with screenshots in the report:
  1. the student scans;
  2. the teacher's live list shows the scan;
  3. the Attendance and Grades tabs load;
  4. the parent's overview loads.
