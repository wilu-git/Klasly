# Spec: Milestone 9 — Worker (Hangfire): close ended sessions + auto-Absent; teacher attendance corrections

> Status: **Approved** (2026-11-10): open questions 1–3 answered as recommended (never-opened session → `Cancelled`; corrections only after the session ends; a small xUnit test project). **Change to Q3 (approved 2026-11-10):** the tests run on a LocalDB `Klasly_Tests` database instead of SQLite in-memory, because the model's SQL Server CHECK syntax (`N'…'`) can't be created on SQLite (see `tasks/plan-m9.md`, decision 5). Plan: `tasks/plan-m9.md` + `tasks/todo-m9.md` (Milestone 8's open checks stay in `tasks/plan.md` / `todo.md`).
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.8 business rules, §27.2 "Auto-absent marking" and "Attendance records & corrections") → `CLAUDE.md` (§3.5 Console Worker, §3.3 Web API, §3.4 Web MVC) → this spec.
> Milestone 8's spec (Android login + Scan QR) is in git history (commit `a2303ce`) and in `docs/reports/2026-11-10-1342-milestone-8-android-login-scan/`.

## What exists today

- **Milestone 8 is built but not fully checked.** The real-phone scan steps in `tasks/todo.md` (Tasks 6–8, Checkpoints C–D) are still open. This milestone does not depend on them, but they should be ticked before Milestone 9 is called done.
- **The Worker is still the template.** `src/Klasly.Worker` has the `Worker.cs` loop that logs every second, no Hangfire, no connection string, and an empty `Jobs/` folder. It already references `Klasly.Core` and `Klasly.Infrastructure`.
- **Sessions:** a teacher creates a session (`Scheduled`), opens it (`Open`) and can end it with **End session** (`Closed`, `ClosedAt` set). Nothing ever sets `Finalized` or `Cancelled`. An `Open` session whose `EndTime` has passed already counts as closed for the QR and scans (`SessionService.IsOpenNow`), but its status stays `Open` in the database.
- **Attendance:** only scans create records (`Source = Scan`). Students who never scan have no record, so the roster shows "Not yet" forever. The `MarkedBy` column and `Source = Manual / Auto` exist in the schema but nothing writes them.
- **No .NET test project exists** (see open question 3).

## Assumptions (correct these before we build)

1. **No database change.** Every column this milestone needs exists (`Status`, `ClosedAt`, `Source`, `MarkedBy`, `MarkedAt`). No migration. The only new tables are Hangfire's own, in the `HangFire` schema (CLAUDE.md §3.1 allows this).
2. **Session status flow:**
   ```
   Scheduled ──open──▶ Open ──End session or EndTime passed──▶ Closed ──Worker marks Absent──▶ Finalized
       └──EndTime passed, never opened──▶ Cancelled   (open question 1)
   ```
   `Finalized` means "attendance is complete": every actively enrolled student has exactly one record. Milestone 11's absence rate counts only `Finalized` sessions (guide §27.8), so this milestone is what makes that number possible.
3. **The Worker tick (`WorkerTickJob`) runs only two steps for now**, in this order, inside one execution:
   1. **Close ended sessions:** every `Open` session with `EndTime ≤ now` → `Closed`, `ClosedAt = now`.
   2. **Mark Absent and finalize:** for every `Closed` session (closed by step 1 *or* by the teacher's **End session**), insert an `Absent` record for each **Active** enrollment that has no record (`Source = Auto`, `MarkedAt = now`, `ScannedAt = null`, `MarkedBy = null`), then set the session to `Finalized`. One `SaveChanges` per session, so a crash never leaves a session half-finalized; the next tick just retries it.

   Steps 3–5 (risk, outbox, Brevo) are added in Milestones 11 and 12. Dropped students get no Absent record.
4. **Running twice is safe.** If a student scanned or a teacher corrected in the moment between "read the roster" and "insert", the unique `(SessionId, StudentId)` index rejects the duplicate. The Worker skips that student and continues; it never overwrites a scan or a correction.
5. **Hangfire setup (CLAUDE.md §3.5):** packages `Hangfire.Core`, `Hangfire.SqlServer`, `Hangfire.NetCore` (latest stable 1.8.x; the exact versions go in the report). SQL Server storage on the `Klasly` connection string, `AddHangfireServer()`, no dashboard, no web host. `WorkerTickJob` is registered on startup with `IRecurringJobManager.AddOrUpdate`, cron from `Worker:Cron` (default `*/5 * * * *`), and carries `[DisableConcurrentExecution]` and `[AutomaticRetry(Attempts = 0)]`. The template `Worker.cs` is deleted.
6. **Worker configuration:** the LocalDB connection string (no password) goes in `src/Klasly.Worker/appsettings.Development.json`, the same string the API uses. `Worker:Cron` goes in `appsettings.json`. Nothing secret is needed yet (the Brevo key comes in Milestone 12).
7. **Where the code lives:**
   - the business logic in a Worker service (`Klasly.Worker/Services/AttendanceFinalizer`), as guide §27.4 says business-rule services stay in the Api and Worker projects;
   - the job class only calls the service;
   - the queries in the existing EF Core repositories in `Klasly.Infrastructure` (a few new methods on `ISessionRepository` / `IAttendanceRepository`), shared with the API.
8. **Times:** the Worker compares UTC with UTC (`DateTime.UtcNow` against `EndTime`). The cron runs in UTC (Hangfire's default); `*/5` means the same thing in any time zone.
9. **Teacher corrections — API:**
   - **New endpoint:** `PUT /api/sessions/{sessionId}/attendance/{studentId}` with body `{ "status": "Present" | "Late" | "Absent" | "Excused" }`. `TeacherOnly` policy, and the session must belong to the caller's class (same ownership check as today's session endpoints).
   - **When:** only after the session has ended, so `Closed` or `Finalized` (open question 2). Any other status → `409` "Attendance can be corrected only after the session has ended."
   - **Who:** the student must be **actively enrolled** in the class, otherwise `404` "This student is not enrolled in this class."
   - **What it writes:** creates the record if there is none, or updates the existing one: `Status` = the new value, `Source = Manual`, `MarkedBy` = the teacher's UserId, `MarkedAt = now`. An existing `ScannedAt` is kept, so the original scan time is not lost.
   - **The same status again** changes nothing and returns `200`.
   - **Response:** `200` with the updated `SessionAttendanceRow`. Bad status → `400` problem-details.
   - **"Logged" (guide §27.2):** the record itself keeps who (`MarkedBy`), when (`MarkedAt`) and how (`Source = Manual`). The API also writes one log line with the session, student, old status → new status and the teacher's UserId. There is no audit-history table, so a second correction replaces the first.
   - The teacher picks Present or Late by hand; the API does not recompute lateness for a correction.
10. **Teacher corrections — MVC:** on the existing session page (`Teacher/Sessions/Details`):
    - once the session is `Closed` or `Finalized`, each roster row gets a status dropdown and a **Save** button (one small form per row, `[ValidateAntiForgeryToken]`), posting to a new `Teacher/Sessions/Correct` action;
    - success → back to the same page with "Attendance updated for {name}."; API error → the API's message on the same page (the existing `TempData["Error"]` pattern);
    - a new **Source** column shows "Scan", "Auto" or "Corrected", so the teacher sees which rows were changed by hand;
    - while the session is `Open`, the page stays exactly as today (QR + 5-second polling, no dropdowns), so polling can't wipe a half-made choice;
    - the session status badge already handles `Finalized` and `Cancelled`.
11. **Corrections to Absent and alerts:** a correction to `Absent` is a normal record with `Source = Manual`. Milestone 12's outbox will pick it up like an `Auto` one (guide §27.8). Nothing is emailed in this milestone.
12. **Out of scope:**
    - risk flags, the outbox, Brevo (Milestones 11–12);
    - excuse requests (Nice-to-Have);
    - a Hangfire dashboard;
    - a correction history table;
    - corrections from the Android app;
    - an admin view of corrections;
    - an attendance page by student or by class across sessions (the session page is the only correction screen).

## Objective

After a class ends, nobody has to remember to close the session or mark who didn't come. Within 5 minutes of the end time (or of **End session**), the Worker closes the session and every enrolled student who didn't scan shows **Absent** on the teacher's page. The session is then `Finalized`, ready for the risk calculation in Milestone 11.

When the record is wrong (a phone died, a student was excused, a scan was refused unfairly), the teacher fixes it on the same page, and the record shows it was corrected and by whom.

Functional requirements covered: guide §27.9 **#5** (auto-Absent after a session closes) and the "Attendance records & corrections" feature in §27.2.

### User stories

- As a **teacher**, I don't have to press **End session**: when the class's end time passes, the Worker closes it within 5 minutes.
- As a **teacher**, after a session ends, the students who didn't scan show as **Absent** with source "Auto" within 5 minutes.
- As a **teacher**, I can change any student's status on an ended session to Present, Late, Absent or Excused, and I see "Corrected" next to the rows I changed.
- As a **teacher**, I can't correct another teacher's session, or a student who isn't in the class.
- As a **student**, a scan that arrives after the session was closed is still refused with "Session is closed" (unchanged).

## API contract (new)

| Call | Body | Success | Errors |
|---|---|---|---|
| `PUT api/sessions/{sessionId}/attendance/{studentId}` (Bearer, Teacher) | `{ "status": "Excused" }` | `200 { studentId, studentNumber, fullName, status, scannedAtUtc, source }` (existing `SessionAttendanceRow`) | `400` bad status · `401` no/bad token · `403` another teacher's class · `404` session not found / student not enrolled · `409` session not ended yet |

## Commands

```bash
dotnet build Klasly.slnx
dotnet run --project src/Klasly.API --launch-profile http        # API (Swagger at /swagger)
dotnet run --project src/Klasly.Web                               # teacher pages
dotnet run --project src/Klasly.Worker                            # Hangfire server, creates the HangFire schema on first run
dotnet list package                                               # all Microsoft.* packages still 10.x

# For the smoke test, tick every minute instead of every 5 (environment variable, not committed):
$env:Worker__Cron = "* * * * *"; dotnet run --project src/Klasly.Worker
```

## Project structure (files this milestone touches)

```
src/Klasly.Worker/
  Klasly.Worker.csproj                 (modified) + Hangfire.Core, Hangfire.SqlServer, Hangfire.NetCore
  Program.cs                           (modified) AddInfrastructure, AddHangfire(SQL Server), AddHangfireServer, register WorkerTickJob
  Worker.cs                            (deleted) template loop
  Jobs/WorkerTickJob.cs                (new) the recurring job: calls the steps in order
  Services/AttendanceFinalizer.cs      (new) close ended sessions; mark Absent + finalize
  appsettings.json                     (modified) Worker:Cron
  appsettings.Development.json         (modified) ConnectionStrings:Klasly (LocalDB, no password)
src/Klasly.Core/
  Interfaces/ISessionRepository.cs     (modified) list Open sessions ended before a time; list Closed sessions
  Interfaces/IAttendanceRepository.cs  (modified) get one record (tracked) for a correction
  DTOs/CorrectAttendanceRequest.cs     (new) { Status } with [Required]
src/Klasly.Infrastructure/Repositories/SessionRepository.cs, AttendanceRepository.cs   (modified)
src/Klasly.API/
  Services/Interfaces/ISessionService.cs, Services/SessionService.cs   (modified) CorrectAttendanceAsync
  Controllers/SessionsController.cs    (modified) PUT api/sessions/{id}/attendance/{studentId}
src/Klasly.Web/
  Repositories/(I)SessionRepository.cs, Services/(I)SessionService.cs  (modified) correct call
  Areas/Teacher/Controllers/SessionsController.cs   (modified) Correct POST action
  Areas/Teacher/Views/Sessions/_Live.cshtml         (modified) Source column; dropdown + Save when ended
  ViewModels/SessionLiveViewModel.cs                (modified if needed) ended flag already there (IsClosed)
```

## Code style

Same layering as the API: the job is thin, the service holds the rule, the repository holds the query.

```csharp
// Jobs/WorkerTickJob.cs
[DisableConcurrentExecution(timeoutInSeconds: 600)]
[AutomaticRetry(Attempts = 0)] // the outbox (Milestone 12) does its own retries
public class WorkerTickJob(AttendanceFinalizer attendance, ILogger<WorkerTickJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var closed = await attendance.CloseEndedSessionsAsync(ct);
        var finalized = await attendance.MarkAbsentAndFinalizeAsync(ct);
        logger.LogInformation("Tick: closed {Closed} sessions, finalized {Finalized}", closed, finalized);
        // Milestone 11: risk flags. Milestone 12: queue alerts, send emails.
    }
}
```

## Testing strategy

- **Build:** `dotnet build Klasly.slnx` with no new warnings; `dotnet list package` shows every `Microsoft.*` package at 10.x (Hangfire must not pull in an older one).
- **Automated tests:** see open question 3.
- **Manual smoke test** on LocalDB with the seed data, the API (`http` profile), the website, and the Worker with `Worker__Cron = "* * * * *"`:
  1. Start the Worker → the `HangFire` schema appears in the `Klasly` database, the log shows a tick every minute, and no Klasly table changed.
  2. Teacher creates a session ending in 3 minutes and opens it; one student scans (or `curl` the scan endpoint). After the end time + 1 tick → the session is `Finalized`, the scanner keeps **Present** (`Scan`), every other active student is **Absent** (`Auto`), and a dropped student has no row.
  3. A second session: teacher presses **End session** early → the next tick marks Absent and finalizes it.
  4. A session created but never opened, ending in 2 minutes → after the end time + 1 tick, it becomes whatever open question 1 decides.
  5. Stop and start the Worker mid-test; run two Worker processes at once → no duplicate records, no errors beyond a logged skip.
  6. On the finalized session page: change an Absent student to **Excused** → "Attendance updated for …", the row shows Excused / Corrected; in the database `Source = Manual`, `MarkedBy` = the teacher's UserId. Change a scanned Present to Late → `ScannedAt` unchanged.
  7. While a session is `Open`: no dropdowns on the page; `PUT` in Swagger → 409 with the message.
  8. Swagger as another teacher → 403; a student not in the class → 404; status `"Sick"` → 400.
  9. Scan after the session closed → "Session is closed" (unchanged).
  10. `/impeccable` on the changed session page (roster with dropdowns); findings fixed or listed in the report.

## Boundaries

- **Always:**
  - business rules in services (Worker service, API `SessionService`), never in the job class or the MVC controller;
  - every write in UTC; Manila time only on screen;
  - ownership checked in the API, not just by hiding the dropdown;
  - `[ValidateAntiForgeryToken]` on the new POST;
  - `dotnet build` after every change, the three reports at the end.
- **Ask first:**
  - any package other than the three Hangfire ones;
  - a database change, a new table (for example, a correction history), or a migration;
  - a test project (open question 3);
  - the Hangfire dashboard;
  - letting the Worker touch `Scheduled` sessions in any way other than what open question 1 decides.
- **Never:**
  - delete or overwrite a scan record from the Worker;
  - model Hangfire's tables in EF Core;
  - add a second retry layer (Hangfire retries stay off);
  - commit a connection string with a password.

## Success criteria

- [x] `dotnet run --project src/Klasly.Worker` starts a Hangfire server that runs `WorkerTickJob` on the `Worker:Cron` schedule, with no dashboard and no web host.
- [x] Within one tick of a session's end (or **End session**), it is `Closed` and then `Finalized`, and every active enrolled student has exactly one attendance record.
- [x] Non-scanners get `Absent` / `Auto`; scans and corrections are never overwritten; running two ticks at once creates no duplicates.
- [x] A teacher can correct a student's status on an ended session from the session page; the record shows `Source = Manual`, `MarkedBy` and `MarkedAt`.
- [x] Corrections are refused for open/scheduled sessions (409), other teachers' sessions (403) and non-enrolled students (404).
- [x] All 10 smoke-test steps behave as written.
- [x] No migration; `dotnet ef migrations has-pending-model-changes` reports none; every Microsoft package still 10.x.
- [x] Reports written; `docs/reports/INDEX.md` updated.

## Open questions (my recommendation first)

1. **What happens to a session that was never opened once its end time passes?** (Status `Scheduled`, nobody could scan.)
   **Recommended:** the Worker sets it to **`Cancelled`** and marks nobody Absent. Students had no way to check in, so counting them absent would be unfair, and guide §27.8 already excludes Cancelled sessions from the absence rate.
   *Alternative A:* treat it like a closed session: everyone Absent, then `Finalized`.
   *Alternative B:* leave it `Scheduled` forever (it then never counts anywhere, and the teacher's session list keeps showing it as upcoming).
2. **When can a teacher correct attendance?**
   **Recommended:** only after the session has ended (`Closed` or `Finalized`). During an open session the page polls every 5 s and students are still scanning, so a correction could race a scan, and the polling would reset a half-chosen dropdown. A student whose phone died is fixed right after **End session**.
   *Alternative:* also while `Open`. Then a manual Present blocks that student's later scan with "Already checked in", and the MVC page needs to pause polling while a dropdown is in use.
3. **Do we add automated tests now?** No .NET test project exists yet, and the Worker's close/Absent logic is the first non-trivial background rule.
   **Recommended:** add a small `tests/Klasly.Worker.Tests` xUnit project with **one** test class for `AttendanceFinalizer`, ~~using the EF Core SQLite in-memory provider~~ **on a LocalDB `Klasly_Tests` database built by the real migrations** (it enforces the unique index, so the "no duplicates" rule is really tested). Packages: `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`. Milestone 14's role-access and boundary tests can then go in the same kind of project.
   *Alternative:* no test project until Milestone 14; rely on the manual smoke test (steps 2–5) for now.
