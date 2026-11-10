# Implementation Plan: Milestone 9 — Worker (Hangfire): close ended sessions + auto-Absent; teacher attendance corrections

> **Plan approved 2026-11-10**, including decision 5 (LocalDB `Klasly_Tests` instead of SQLite). Built from [`SPEC.md`](../SPEC.md). The spec's open questions were answered 2026-11-10: (1) a never-opened session → `Cancelled`; (2) corrections only after the session ends; (3) add a small xUnit test project.
> Task details, acceptance criteria and verification steps are in [`todo-m9.md`](./todo-m9.md).
> **Milestone 8's plan stays in `plan.md` / `todo.md`** because its real-phone checks are still open (team decision 2026-11-10). `/build` defaults to `todo.md`, so point it at `tasks/todo-m9.md` when building Milestone 9.

## Overview

The Worker becomes a Hangfire server with one recurring job, `WorkerTickJob`. Each tick it closes sessions past their end time, cancels sessions that were never opened, marks every actively enrolled non-scanner `Absent` (`Source = Auto`), and sets the session to `Finalized`. Teachers get a correction endpoint in the API and a status dropdown on the ended-session page in MVC. There is no database change.

## Architecture decisions

1. **Risky part first.** Task 1 only stands up Hangfire with an empty job, so any problem with the packages, SQL storage or the cron shows up before any business logic is written.
2. **Hangfire 1.8.25** (`Hangfire.Core`, `Hangfire.SqlServer`, `Hangfire.NetCore`, the latest stable on NuGet, 2026-11-10). Its `netstandard2.0` builds have no SQL client dependency of their own. `Microsoft.Data.SqlClient` already comes through `Klasly.Infrastructure` → EF Core SqlServer 10.x. Hangfire.NetCore asks only for `Microsoft.Extensions.*.Abstractions` ≥ 2.0, so NuGet uses the 10.x versions the Worker already has. **Check:** `dotnet list src/Klasly.Worker package --include-transitive` shows no `Microsoft.*` below 10.x.
3. **One service, two methods.** `Klasly.Worker/Services/AttendanceFinalizer`:
   - `CloseEndedSessionsAsync`: `Open` past its end → `Closed` (`ClosedAt = now`); `Scheduled` past its end → `Cancelled`. One `SaveChanges`.
   - `MarkAbsentAndFinalizeAsync`: per `Closed` session, add the missing `Absent` rows and set `Finalized` in **one** `SaveChanges`.

   `WorkerTickJob` calls them in order and logs the counts. The job is resolved in a DI scope per run (Hangfire.NetCore's activator), so it gets its own `KlaslyDbContext`.
4. **A lost race stops the tick, it doesn't break it.** After a session is `Closed`, scans are refused, so the only possible race is a teacher saving a correction at the same moment as the Worker. If the unique `(SessionId, StudentId)` index rejects the batch:
   - a new `IAttendanceRepository.TrySaveAsync` returns `false` and clears the change tracker, like the existing `TryAddAsync` handles SQL errors 2601/2627;
   - the Worker logs a warning and stops finalizing for this tick;
   - the next tick reads the roster again, now including the teacher's row, and finishes.

   A scan or correction is never overwritten.
5. **Tests on a LocalDB test database, not SQLite (change to answer 3).** The approved answer said "SQLite in-memory", but the model can't be created on SQLite: its CHECK constraints use SQL Server syntax (`[Status] IN (N'Present', …)`; SQLite rejects the `N'…'` strings) and `SYSUTCDATETIME()` defaults.
   - **Recommended instead:** the test fixture runs the real migrations on a separate LocalDB database, `Klasly_Tests` (dropped and re-created once per test run), and seeds its own rows.
   - **Why:** this tests the real schema, the real unique index and the real SQL error 2601/2627 path, and needs **no** extra EF package (only `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.11.0).
   - **Cost:** the tests need LocalDB, which this PC already uses.
   - **Connection string:** it has no password, so it stays a constant in the test fixture. It never points at the real `Klasly` database.
6. **Corrections live in the API's `SessionService`.** It already holds `GetOwnSessionAsync` (the ownership check) and the roster mapping, so the new method returns the same `SessionAttendanceRow`. The correction writes one `ILogger` line (session, student, old → new status, teacher UserId).
7. **MVC decides "can correct" from the session status, not from "no QR".** `SessionLiveViewModel.IsClosed` is also true for an `Open` session past its end that the Worker hasn't closed yet, and the API refuses that (409). So a new `CanCorrect` flag is set only when the status is `Closed` or `Finalized`. The 5-second poll only runs while the session is live, so it never shows dropdowns.
8. **No migration.** `dotnet ef migrations has-pending-model-changes -p src/Klasly.Infrastructure -s src/Klasly.API` must say no changes.

## Dependency graph

```
Task 1 Hangfire host + empty job ─┬─▶ Task 3 close / cancel ended ─▶ Task 4 Absent + finalize ─▶ Checkpoint A (Worker)
Task 2 test project + fixture ────┘                                                                    │
                                    Task 5 API correction endpoint (needs Closed/Finalized sessions) ◀─┘ ─▶ Checkpoint B
                                    Task 6 MVC Source column + Cancelled text ─▶ Task 7 MVC correction form ─▶ Task 8 polish + reports ─▶ Checkpoint C
```

Tasks 1 and 2 don't depend on each other. Task 5 only needs real `Closed`/`Finalized` sessions to test against (Checkpoint A). Task 6 could start after Task 3, but it is kept in order for one builder.

## Task list

### Phase 1: Worker foundation
- [x] Task 1: Hangfire server with an empty `WorkerTickJob` on the configured cron
- [x] Task 2: `tests/Klasly.Worker.Tests` with the LocalDB fixture

### Phase 2: Worker rules (test first)
- [x] Task 3: Close ended Open sessions, cancel never-opened ones
- [x] Task 4: Mark non-scanners Absent and finalize

### Checkpoint A: Worker
- [x] `dotnet build Klasly.slnx` and `dotnet test` pass; spec smoke steps 1–5 pass

### Phase 3: Corrections
- [x] Task 5: API `PUT api/sessions/{id}/attendance/{studentId}`

### Checkpoint B: API
- [x] Spec smoke steps 7–8 (API part) pass in Swagger

- [x] Task 6: MVC Source column + "never opened" message
- [x] Task 7: MVC correction dropdown + Save
- [ ] Task 8: `/impeccable`, full smoke test, reports

### Checkpoint C: Complete
- [ ] Every success criterion in SPEC.md is met; ready for `/review`

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Hangfire.SqlServer picks the wrong SQL client or can't connect | High: no Worker | Task 1 tests it first. Fallback: `UseSqlServerStorage(() => new Microsoft.Data.SqlClient.SqlConnection(cs))` |
| Hangfire pulls an old `Microsoft.Extensions.*` package | Med: breaks CLAUDE.md §4.1 | `dotnet list package --include-transitive` in Task 1; if needed, add the matching 10.x package explicitly |
| Two Worker processes (or a tick that overlaps the next) | Med: duplicate work | `[DisableConcurrentExecution]` + the unique index + decision 4; tested in smoke step 5 |
| `Klasly_Tests` fixture accidentally pointed at `Klasly` | High: test data in the dev DB | Hard-coded database name `Klasly_Tests`, and the fixture asserts the name before dropping |
| The Worker closes a session while the teacher's page is polling | Low | The page already stops polling when the QR call returns 409 (Milestone 7); a reload shows the final roster |
| Corrections in MVC racing the Worker's finalize | Low | Decision 4: the Worker retries next tick; the teacher's correction wins |

## Open questions

None. Decision 5 approved 2026-11-10.
