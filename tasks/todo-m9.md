# Tasks: Milestone 9 — Worker (Hangfire) + attendance corrections

> From [`plan-m9.md`](./plan-m9.md) and [`SPEC.md`](../SPEC.md). Every task ends with `dotnet build Klasly.slnx` (and `dotnet test --solution Klasly.slnx` once Task 2 exists).
> Smoke-test steps refer to the spec's Testing strategy. For Worker checks, run it with `$env:Worker__Cron = "* * * * *"`.

---

## Task 1: Hangfire server with an empty `WorkerTickJob`

**Description:**
- Add `Hangfire.Core`, `Hangfire.SqlServer`, `Hangfire.NetCore` 1.8.25.
- In `Program.cs`: `AddInfrastructure(connectionString)`, `AddHangfire(c => c.UseSqlServerStorage(connectionString))` (plus the recommended serializer settings), `AddHangfireServer()`.
- After `Build()`: `IRecurringJobManager.AddOrUpdate<WorkerTickJob>("worker-tick", j => j.RunAsync(CancellationToken.None), cron)`, with the cron from `Worker:Cron`.
- `WorkerTickJob` carries `[DisableConcurrentExecution(600)]` and `[AutomaticRetry(Attempts = 0)]`, and only logs "Tick" for now.
- Delete the template `Worker.cs`.
- Add the LocalDB connection string to `appsettings.Development.json` and `Worker:Cron = */5 * * * *` to `appsettings.json`.
- Startup fails with a clear message if the connection string is missing.

**Acceptance criteria:**
- [x] `dotnet run --project src/Klasly.Worker` creates the `HangFire` schema in `Klasly` and logs a tick every minute with `Worker__Cron = "* * * * *"`. *(11 HangFire tables; ticks at 06:50:02Z and 06:51:01Z.)*
- [x] No Klasly table changed; no dashboard or web host. *(Row counts of all Klasly tables identical before/after.)*
- [x] `dotnet list src/Klasly.Worker package --include-transitive`: every `Microsoft.*` package at 10.x. *(All `Microsoft.Extensions.*` / `EntityFrameworkCore.*` at 10.0.14; non-framework ones like `Bcl.AsyncInterfaces` 8.0.0 were already in the API via EF Core Design.)*

**Verification:**
- [x] Build succeeds: `dotnet build src/Klasly.Worker` and `src/Klasly.Web`, 0 warnings. *(Full `Klasly.slnx` build blocked only by the running API locking its DLLs; re-run when it's stopped.)*
- [x] Manual check: spec smoke step 1

**Dependencies:** None

**Files likely touched:**
- `src/Klasly.Worker/Klasly.Worker.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json` (modified)
- `src/Klasly.Worker/Jobs/WorkerTickJob.cs` (new), `src/Klasly.Worker/Worker.cs` (deleted)

**Estimated scope:** Medium (5 files)

---

## Task 2: Test project with a LocalDB fixture

**Description:**
- Create `tests/Klasly.Worker.Tests` (xUnit v3, `net10.0`) referencing `Klasly.Worker` and `Klasly.Infrastructure`, and add it to `Klasly.slnx`.
- A shared fixture builds `KlaslyDbContext` on `(localdb)\MSSQLLocalDB`, database **`Klasly_Tests`**. It asserts that name, then runs `EnsureDeleted()` + `Migrate()` once per run.
- Tests share the fixture and each one works on its own class/session rows, so tests don't interfere.
- One first test: the migrated database has the seeded `SystemSettings` rows.

**Acceptance criteria:**
- [x] `dotnet test` runs and passes the first test; `Klasly_Tests` exists afterwards; `Klasly` is untouched. *(.NET 10 SDK needed `"test": { "runner": "Microsoft.Testing.Platform" }` in `global.json`; the command is now `dotnet test --solution Klasly.slnx`. Klasly row counts identical before/after.)*
- [x] Packages listed with versions in the report (`xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.11.0).

**Verification:**
- [x] Tests pass: `dotnet test Klasly.slnx`
- [x] Build succeeds: `dotnet build Klasly.slnx`

**Dependencies:** None (needs the plan's decision 5 approved)

**Files likely touched:**
- `tests/Klasly.Worker.Tests/Klasly.Worker.Tests.csproj`, `DatabaseFixture.cs`, `DatabaseFixtureTests.cs` (new)
- `Klasly.slnx` (modified)

**Estimated scope:** Small (4 files)

---

## Task 3: Close ended sessions, cancel never-opened ones (test first)

**Description:** Write the tests first, then `AttendanceFinalizer.CloseEndedSessionsAsync(ct)`:
- every `Open` session with `EndTime ≤ now` → `Closed`, `ClosedAt = now`;
- every `Scheduled` session with `EndTime ≤ now` → `Cancelled` (spec answer 1);
- one `SaveChanges`; returns the counts.

This needs a new `ISessionRepository.ListEndedAsync(DateTime nowUtc, ct)` (tracked; `Open` or `Scheduled`, `EndTime ≤ now`). `WorkerTickJob` calls the method and logs the counts.

**Acceptance criteria:**
- [x] Tests:
  - Open past its end → Closed with ClosedAt;
  - Scheduled past its end → Cancelled;
  - Open/Scheduled not yet ended, and Closed/Finalized/Cancelled sessions → untouched.
- [x] A session ending exactly at `now` counts as ended (`≤`).
- [x] Running twice changes nothing the second time.

**Verification:**
- [x] Tests pass: `dotnet test Klasly.slnx`
- [x] Build succeeds: `dotnet build Klasly.slnx`

**Dependencies:** Tasks 1, 2

**Files likely touched:**
- `src/Klasly.Core/Interfaces/ISessionRepository.cs`, `src/Klasly.Infrastructure/Repositories/SessionRepository.cs` (modified)
- `src/Klasly.Worker/Services/AttendanceFinalizer.cs` (new), `Jobs/WorkerTickJob.cs`, `Program.cs` (register the service) (modified)
- `tests/Klasly.Worker.Tests/AttendanceFinalizerTests.cs` (new)

**Estimated scope:** Medium (5–6 files, small edits)

---

## Task 4: Mark non-scanners Absent and finalize (test first)

**Description:** Write the tests first, then `AttendanceFinalizer.MarkAbsentAndFinalizeAsync(ct)`. For each `Closed` session:
- load the **Active** enrollments and the existing records;
- add an `Absent` record for each student without one (`Source = Auto`, `MarkedAt = now`, `ScannedAt = null`, `MarkedBy = null`);
- set `Status = Finalized`;
- save everything in **one** `SaveChanges` via a new `IAttendanceRepository.TrySaveAsync`. On a unique-index violation (2601/2627), it clears the change tracker and returns `false`; the service logs a warning and stops for this tick (plan decision 4).

New repository members: `ISessionRepository.ListClosedAsync(ct)` (tracked) and `IAttendanceRepository.Add(record)` (tracks only).

**Acceptance criteria:**
- [x] Tests:
  - 3 active students, 1 scanned → scanner keeps Present/Scan, 2 get Absent/Auto, session Finalized;
  - a Dropped student gets no row;
  - a session with a Manual correction keeps it;
  - a session with no enrollments still becomes Finalized;
  - a second run adds nothing.
- [x] Test: a row inserted between "read" and "save" (simulated with a second `DbContext`) → `TrySaveAsync` false, session stays Closed; the next run finalizes it without touching that row.

**Verification:**
- [x] Tests pass: `dotnet test Klasly.slnx`
- [x] Build succeeds: `dotnet build Klasly.slnx`

**Dependencies:** Task 3

**Files likely touched:**
- `src/Klasly.Core/Interfaces/ISessionRepository.cs`, `IAttendanceRepository.cs` (modified)
- `src/Klasly.Infrastructure/Repositories/SessionRepository.cs`, `AttendanceRepository.cs` (modified)
- `src/Klasly.Worker/Services/AttendanceFinalizer.cs`, `Jobs/WorkerTickJob.cs` (modified)
- `tests/Klasly.Worker.Tests/AttendanceFinalizerTests.cs` (modified)

**Estimated scope:** Medium (6 files, small edits)

### Checkpoint A: Worker (after Tasks 1–4)
- [x] `dotnet build Klasly.slnx` and `dotnet test Klasly.slnx` pass
- [x] Spec smoke steps 1–5 pass against the real `Klasly` database (API + website + Worker running) *(2026-11-10 09:27–09:33Z: sessions 1014–1018; two Workers at once ran each tick once, 0 duplicates, 0 errors.)*
- [ ] Review with the team before continuing

---

## Task 5: API correction endpoint

**Description:**
- **Endpoint:** `PUT api/sessions/{id}/attendance/{studentId}` in `SessionsController` (`TeacherOnly`), body `CorrectAttendanceRequest { [Required] AttendanceStatus? Status }`.
- **Rules in `SessionService.CorrectAttendanceAsync`:**
  - the caller owns the session (`GetOwnSessionAsync`, which gives 404/403);
  - the session is `Closed` or `Finalized`, otherwise `409` "Attendance can be corrected only after the session has ended.";
  - the student is actively enrolled, otherwise `404` "This student is not enrolled in this class.".
- **Writes:**
  - an existing record (from a new tracked `IAttendanceRepository.GetAsync(sessionId, studentId)`) → `Status`, `Source = Manual`, `MarkedBy`, `MarkedAt = now`, `ScannedAt` kept;
  - no record → insert via the existing `TryAddAsync` (a lost race → 409 "Attendance was just updated. Reload and try again.");
  - the same status → no change, `200`.
- **Log:** one line with the session, student, old → new status and the teacher's UserId.
- **Returns:** the updated `SessionAttendanceRow`.

**Acceptance criteria:**
- [x] Swagger:
  - Absent → Excused on a Finalized session = 200, DB shows Manual + MarkedBy;
  - Present → Late keeps ScannedAt;
  - an Open session → 409; another teacher → 403; a student not in the class → 404; `"Sick"` → 400.
- [x] The API log shows the correction line. *(Checked with curl against the same endpoints instead of the Swagger page: 409 / 403 / 404 / 400 and 200 as listed.)*

**Verification:**
- [x] Build succeeds: `dotnet build Klasly.slnx`; `dotnet test` still passes
- [x] Manual check: spec smoke steps 6 (DB part), 7 (API part) and 8 in Swagger

**Dependencies:** Checkpoint A (needs Closed/Finalized sessions)

**Files likely touched:**
- `src/Klasly.Core/DTOs/CorrectAttendanceRequest.cs` (new)
- `src/Klasly.Core/Interfaces/IAttendanceRepository.cs`, `src/Klasly.Infrastructure/Repositories/AttendanceRepository.cs` (modified)
- `src/Klasly.API/Services/Interfaces/ISessionService.cs`, `Services/SessionService.cs`, `Controllers/SessionsController.cs` (modified)

**Estimated scope:** Medium (6 files, small edits)

### Checkpoint B: API (after Task 5)
- [x] All Swagger checks above pass; `dotnet build` + `dotnet test` clean

---

## Task 6: MVC Source column + "never opened" message

**Description:** On the session page roster (`_Live.cshtml`), add a **Source** column: "Scan", "Auto" (marked by Klasly), "Corrected" (Manual), "—" when there's no record. A `Cancelled` session shows "This session was never opened, so no attendance was taken." instead of the "ended" alert, and no roster.

**Acceptance criteria:**
- [x] A Finalized session shows Present/Scan and Absent/Auto rows in Manila time; an Open session shows Scan rows while polling, unchanged otherwise.
- [x] A Cancelled session shows the "never opened" message.

**Verification:**
- [x] Build succeeds: `dotnet build Klasly.slnx`
- [x] Manual check: open a Finalized, an Open and a Cancelled session in the browser

**Dependencies:** Checkpoint A

**Files likely touched:**
- `src/Klasly.Web/Areas/Teacher/Views/Sessions/_Live.cshtml`, `Details.cshtml` (modified)

**Estimated scope:** Small (2 files)

---

## Task 7: MVC correction dropdown + Save

**Description:**
- **API plumbing:** `CorrectAttendanceAsync` on the Web `ISessionRepository` (PUT) and `ISessionService`.
- **View model:** a `CanCorrect` flag on `SessionLiveViewModel`, set in `GetSessionAsync` only when the status is `Closed` or `Finalized`; always false in the 5-second poll.
- **View:** when `CanCorrect`, each roster row gets a small form: a status `<select>` (Present, Late, Absent, Excused, current one selected, labelled for screen readers with the student's name) and a **Save** button.
- **Controller:** the form posts to `Teacher/Sessions/Correct` (`[HttpPost, ValidateAntiForgeryToken]`) with `id`, `studentId`, `status`. Success → `TempData["Success"] = "Attendance updated for {name}."`; an API form error → `TempData["Error"]` with the API's message (the existing `ChangeAsync` pattern). Either way, redirect back to the page.

**Acceptance criteria:**
- [x] On a Finalized session: change Absent → Excused → success message; the row shows Excused / Corrected.
- [x] On an Open session: no dropdowns. On a session closed in another tab after the page loaded: the API's 409 message shows. *(Open part checked in the browser. Dropdowns only render for Closed/Finalized, which never go back to Open, so the 409 path was checked through the API only.)*
- [x] Every POST has an anti-forgery token; keyboard-only use works (Tab to select, Tab to Save, Enter).

**Verification:**
- [x] Build succeeds: `dotnet build Klasly.slnx`
- [x] Manual check: spec smoke steps 6 and 7 in the browser

**Dependencies:** Tasks 5, 6

**Files likely touched:**
- `src/Klasly.Web/Repositories/Interfaces/ISessionRepository.cs`, `Repositories/SessionRepository.cs` (modified)
- `src/Klasly.Web/Services/Interfaces/ISessionService.cs`, `Services/SessionService.cs` (modified)
- `src/Klasly.Web/ViewModels/SessionLiveViewModel.cs`, `Areas/Teacher/Controllers/SessionsController.cs`, `Areas/Teacher/Views/Sessions/_Live.cshtml` (modified)
- `src/Klasly.Web/Models/Api/SessionDtos.cs` (modified: request record)

**Estimated scope:** Large by file count (8), but each edit is a few lines that follow the existing Open/Close pattern. If it grows, split the API plumbing (repository + service) from the view.

---

## Task 8: `/impeccable`, full smoke test, reports

**Description:**
- Run `/impeccable` on the session page with the correction dropdowns and fix what it finds.
- Run all 10 spec smoke steps.
- Run `dotnet ef migrations has-pending-model-changes -p src/Klasly.Infrastructure -s src/Klasly.API`, which must report none, and `dotnet list package`.
- Write the three reports (with an `erDiagram` only if the DB changed, which it shouldn't) and the INDEX line.

**Acceptance criteria:**
- [ ] `/impeccable` findings fixed or listed in the report.
- [ ] All 10 smoke steps pass.
- [ ] No pending model changes.
- [ ] All `Microsoft.*` packages are 10.x, and the Hangfire and test package versions are in the report.

**Verification:**
- [ ] Tests pass: `dotnet test Klasly.slnx`; Build succeeds: `dotnet build Klasly.slnx`
- [ ] Reports in `docs/reports/<yyyy-MM-dd-HHmm>-milestone-9-worker-absent-corrections/`, one line in `docs/reports/INDEX.md`

**Dependencies:** Task 7

**Files likely touched:**
- `src/Klasly.Web/Areas/Teacher/Views/Sessions/*.cshtml` (polish only)
- `docs/reports/<folder>/DEVELOPER-DOCS.md`, `HOW-TO-USE.md`, `FLOWCHARTS.md`, `docs/reports/INDEX.md`

**Estimated scope:** Medium

### Checkpoint C: Complete
- [ ] Every success criterion in SPEC.md is met
- [ ] No migration; Hangfire tables only in the `HangFire` schema
- [ ] Ready for `/review`
