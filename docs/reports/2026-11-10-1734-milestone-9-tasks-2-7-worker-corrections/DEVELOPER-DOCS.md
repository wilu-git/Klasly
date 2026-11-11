# Milestone 9 Tasks 2–7 — Worker closes and finalizes sessions; teachers correct attendance

## Summary

This built Milestone 9 Tasks 2–7 from `tasks/todo-m9.md`:
- **Tests:** a new xUnit project runs on its own LocalDB database.
- **Worker:** every tick now closes ended sessions, cancels sessions that were never opened, marks every active non-scanner **Absent** and sets the session to **Finalized**.
- **Teachers:** they can correct a student's result after a session ends, through a new API endpoint and a status dropdown on the MVC session page.

There is no database change. Task 8 (`/impeccable`, the full 10-step smoke test re-run, the final reports) is still open.

## Projects touched

CONSOLE WORKER, WEB API, WEB MVC, EF CORE (repositories only), tests (new project). MSSQL: no schema change.

## Files changed

| Path | Change | Why |
|---|---|---|
| `global.json` | modified | `"test": { "runner": "Microsoft.Testing.Platform" }`: the .NET 10 SDK refuses the old VSTest runner with xUnit v3 4.x |
| `Klasly.slnx` | modified | Adds `tests/Klasly.Worker.Tests` in a `/tests/` folder |
| `tests/Klasly.Worker.Tests/Klasly.Worker.Tests.csproj` | added | xUnit v3 test project referencing the Worker and Infrastructure |
| `tests/Klasly.Worker.Tests/DatabaseFixture.cs` | added | One `Klasly_Tests` LocalDB database per test run, built by the real migrations; refuses to drop any other name |
| `tests/Klasly.Worker.Tests/DatabaseFixtureTests.cs` | added | The migrated test DB has the 5 seeded settings |
| `tests/Klasly.Worker.Tests/AttendanceFinalizerTests.cs` | added | 10 tests for close/cancel, Absent/finalize and the lost-race case |
| `src/Klasly.Worker/Services/AttendanceFinalizer.cs` | added | The two Worker attendance steps |
| `src/Klasly.Worker/Jobs/WorkerTickJob.cs` | modified | Calls the two steps in order and logs the counts |
| `src/Klasly.Worker/Program.cs` | modified | Registers `AttendanceFinalizer` |
| `src/Klasly.Core/Interfaces/ISessionRepository.cs` | modified | + `ListEndedAsync`, `ListClosedAsync` |
| `src/Klasly.Core/Interfaces/IAttendanceRepository.cs` | modified | + `GetAsync`, `Add`, `TrySaveAsync` |
| `src/Klasly.Infrastructure/Repositories/SessionRepository.cs` | modified | Implements the two queries |
| `src/Klasly.Infrastructure/Repositories/AttendanceRepository.cs` | modified | Implements the three members; `TrySaveAsync` turns SQL 2601/2627 into `false` |
| `src/Klasly.Core/DTOs/CorrectAttendanceRequest.cs` | added | Body of the correction endpoint (`[Required] Status`) |
| `src/Klasly.API/Services/SessionService.cs` (+ interface) | modified | `CorrectAttendanceAsync` with the rules and one log line |
| `src/Klasly.API/Controllers/SessionsController.cs` | modified | `PUT api/sessions/{id}/attendance/{studentId}` |
| `src/Klasly.Web/Models/Api/SessionDtos.cs` | modified | `CorrectAttendanceRequest` record |
| `src/Klasly.Web/Repositories/SessionRepository.cs` (+ interface) | modified | PUT call |
| `src/Klasly.Web/Services/SessionService.cs` (+ interface) | modified | Pass-through; `CanCorrect` only for Closed/Finalized; no roster for Cancelled |
| `src/Klasly.Web/ViewModels/SessionLiveViewModel.cs` | modified | `CanCorrect` flag and the status list |
| `src/Klasly.Web/Areas/Teacher/Controllers/SessionsController.cs` | modified | `Correct` POST action (anti-forgery) |
| `src/Klasly.Web/Areas/Teacher/Views/Sessions/_Live.cshtml` | modified | Source column; dropdown + Save per row when `CanCorrect` |
| `src/Klasly.Web/Areas/Teacher/Views/Sessions/Details.cshtml` | modified | "Never opened" message for Cancelled sessions |
| `tasks/todo-m9.md`, `tasks/plan-m9.md` | modified | Tasks 2–7 ticked with evidence notes |

## Key code

**Worker tick: every step uses the same "now".**

```csharp
var now = DateTime.UtcNow;
var (closed, cancelled) = await finalizer.CloseEndedSessionsAsync(now, ct);
var finalized = await finalizer.MarkAbsentAndFinalizeAsync(now, ct);
```

**Finalize: one save per session, and a lost race stops the tick** (plan decision 4).

```csharp
foreach (var enrollment in absentees)
    attendance.Add(new AttendanceRecord { SessionId = session.SessionId, StudentId = enrollment.StudentId,
        Status = AttendanceStatus.Absent, Source = AttendanceSource.Auto, MarkedAt = nowUtc });
session.Status = SessionStatus.Finalized;

if (!await attendance.TrySaveAsync(ct)) { logger.LogWarning(...); break; } // next tick re-reads and finishes
```

**`TrySaveAsync`** (Infrastructure): a duplicate-key error (2601/2627) clears the change tracker and returns `false`. The whole batch, including the status change, was rolled back.

**Correction rules** (`SessionService.CorrectAttendanceAsync`):

| Check | Result |
|---|---|
| Not the caller's class | 403 (`GetOwnSessionAsync`) |
| Status not Closed/Finalized | 409 "Attendance can be corrected only after the session has ended." |
| Student not actively enrolled | 404 "This student is not enrolled in this class." |
| Same status as now | 200, nothing written, no log line |
| Existing record | `Status`, `Source = Manual`, `MarkedBy` (teacher's UserId), `MarkedAt = now`; `ScannedAt` kept |
| No record | Insert via `TryAddAsync`; a lost race → 409 "Attendance was just updated. Reload and try again." |
| Unknown status (`"Sick"`) | 400 (JSON enum converter) |

Log line: `Attendance corrected: session 1014, student 1, Absent -> Excused by user 2`.

**One deviation from the plan:** the service methods take `DateTime nowUtc` instead of only `ct`. That lets the tests check the exact `EndTime == now` boundary without adding a fake-clock package.

## Database changes

None. `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration." The tests create and drop a separate LocalDB database, `Klasly_Tests`.

## How it was verified

- `dotnet build Klasly.slnx`: succeeded, 0 warnings.
- `dotnet test --solution Klasly.slnx`: **11 passed, 0 failed**.
  - Task 3: ended Open → Closed with `ClosedAt`, ended Scheduled → Cancelled, ending exactly at now counts, other sessions untouched, second run changes nothing.
  - Task 4: scanner kept with Absent for the rest, dropped student gets no row, Manual correction kept, no enrollments still finalizes, second run adds nothing, and the lost race (a second `DbContext` inserts between read and save).
- Packages (test project): `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.11.0. `dotnet list Klasly.slnx package --include-transitive`: no `Microsoft.AspNetCore/EntityFrameworkCore/Extensions.*` package below 10.x. Hangfire stays at 1.8.25 (Task 1).
- **Smoke test** on the real `Klasly` database, 2026-11-10 09:26–09:33 UTC (API + website from the preview pane, Worker with `Worker__Cron = "* * * * *"`). API calls were made with curl instead of the Swagger page.

  | Step | Result |
  |---|---|
  | 1 | The Worker ticks every minute |
  | 2 | Session 1014: Bryan scanned → Present/Scan; the other 5 → Absent/Auto; Finalized. Session 1017 (class 3): the dropped student has no row |
  | 3 | Session 1015, ended early → finalized on the next tick |
  | 4 | Session 1016, never opened → Cancelled; its page shows "This session was never opened, so no attendance was taken." |
  | 5 | Two Workers at once: each tick ran once; session 1018 finalized with 6 rows; 0 duplicate rows in the whole table; 0 errors in both logs. One Worker stopped mid-run and the other kept ticking |
  | 6 | Browser, keyboard only: Absent → Excused showed "Attendance updated for Aira Mendoza." and Excused / Corrected; DB `Manual`, `MarkedBy = 2`. Present → Late kept `ScannedAt` 09:26:46 |
  | 7 | Open session: no dropdowns; PUT → 409 with the message |
  | 8 | Another teacher → 403; student 7 (not in the class) → 404; `"Sick"` → 400 |
  | 9 | A real QR token used right after ending the session → 409 "Session is closed" |

- Side effect of the smoke test: the first tick also closed 10 old dev sessions and finalized 16 (old Open sessions past their end, plus the Closed ones), giving them Absent rows. That's the intended Worker behaviour on old data.

## Known limits / TODO

- **Task 8 is open:** run `/impeccable` on the session page, do the full smoke re-run and write the final milestone reports.
- At about 800 px wide, the roster table scrolls sideways to reach the Save buttons. That's for the `/impeccable` pass.
- "Session closed in another tab → 409 in MVC" can't happen through the dropdowns (they only show for Closed/Finalized, which never reopen), so it was checked through the API only.
- The tests need LocalDB on the machine.
- `dotnet-ef` is 10.0.11, and the runtime is 10.0.14 (warning only).
- The "Review with the team" item at Checkpoint A is still open.
- Nothing is committed yet.
