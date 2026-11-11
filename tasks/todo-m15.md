# Milestone 15 — Tasks (Review of the whole solution; fix findings)

> Plan: [`plan-m15.md`](./plan-m15.md) · Spec: [`SPEC.md`](../SPEC.md) · Findings: [`review-m15.md`](./review-m15.md) (created in Task 1).
> Tasks 1–6 are **read-only**: they add rows to `review-m15.md` and change no source file.
> After every fix task (7–11):
> - `dotnet build Klasly.slnx` and `dotnet test Klasly.slnx` (LocalDB must be running);
> - for Android, `./gradlew assembleDebug testDebugUnitTest`;
> - after package changes, `dotnet list package`;
> - a report folder in `docs/reports/` + a line in `docs/reports/INDEX.md` (CLAUDE.md §5), and `graphify update .`.

---

## Task 1: Baseline and findings skeleton

**Description:** Record the solution's current state before anything changes, so "no worse than baseline" can be measured. Then create `tasks/review-m15.md` with the checklist, the severity table, an empty findings table and the two empty traceability tables.

**Acceptance criteria:**
- [x] The `review-m15.md` Baseline section records:
  - the `dotnet build` warning count (per project);
  - the `dotnet test` pass/fail/skip counts;
  - the result of `./gradlew assembleDebug testDebugUnitTest lintDebug` (lint warning count);
  - the output of `dotnet list package`, `--vulnerable --include-transitive` and `--deprecated`.
- [x] The secrets scan is done and its result written down:
  - tracked files and `git log -p --all` checked for `Jwt:Key`, `Qr:SigningKey`, the Brevo `api-key`, `private_key` (FCM JSON) and `Password=` in connection strings;
  - `google-services.json` and `local.properties` confirmed git-ignored and never committed.
- [x] `dotnet ef migrations script` is written to the scratchpad (not the repo), ready for Task 2.
- [x] Rows `F-01` (Serilog not installed, High, CLAUDE.md §3.3 / guide §16) and the 9 `shortcut:` rows are entered. The 3 deprecated Android API rows are already marked Low, won't fix (Q3).

**Verification:**
- [x] `review-m15.md` exists with all the sections above; `git status` shows no source changes.

**Dependencies:** None

**Files likely touched:**
- `tasks/review-m15.md` (new)

**Estimated scope:** S

---

## Task 2: Review `Klasly.Core` + `Klasly.Infrastructure`

**Description:** Check the entities, the Fluent API configurations, the migrations, the seed and the EF Core repositories. Compare the migration script with `docs/database/Klasly_Schema.sql`.

**Acceptance criteria:**
- [x] Each §27.7 convention is checked against the script from Task 1, and every difference becomes a row. Hangfire tables are ignored. The conventions are:
  - 17 tables;
  - enums stored as strings, with a CHECK for each;
  - every CHECK from the SQL file;
  - every unique rule, plus the filtered index for "one active term";
  - `NoAction` on every FK;
  - `SYSUTCDATETIME()` defaults on `datetime2(0)` columns;
  - the 5 listed indexes;
  - the 5 `SystemSettings` seed rows.
- [x] Repositories are checked for:
  - data access only (no business rules);
  - `AsNoTracking` on reads;
  - no N+1 queries;
  - `async` + `CancellationToken`;
  - no hard delete (`Remove`/`ExecuteDelete`);
  - UTC everywhere.
- [x] The seed is checked for fictional data only (RA 10173), and `Klasly.Core` for having no packages.

**Verification:**
- [x] The Core/Infrastructure section of `review-m15.md` is complete; no source changes.

**Dependencies:** Task 1

**Files likely touched:**
- `tasks/review-m15.md`

**Estimated scope:** M

---

## Task 3: Review `Klasly.API`

**Description:** Review the security boundary by hand. At the same time, one `agent-skills:security-auditor` agent reviews the same project in the background. I check each agent finding against the code before it becomes a row.

**Acceptance criteria:**
- [x] **Every controller action** is checked for:
  - the right policy;
  - an ownership check in the service (§27.5);
  - DTOs in and out, never entities;
  - the status codes and messages from `CLAUDE.md` §3.3 and §27.8.
- [x] **Configuration** is checked for:
  - CORS off;
  - Swagger/OpenAPI in Development only;
  - the fallback policy in place;
  - JWT validation settings (issuer, audience, lifetime, key length);
  - the QR HMAC compared in constant time;
  - password hashing with the Identity hasher;
  - no secrets in any `appsettings*.json`.
- [x] **Business rules in the services:**
  - score ≤ MaxScore;
  - weights total 100;
  - Present/Late;
  - the 1-step grace;
  - duplicate scan → 409, through the unique constraint;
  - grade-posted push logged in Notifications.
- [x] **Error handling:** `ApiExceptionHandler` never leaks exception details, and no `catch` block silently swallows an error (§15).
- [x] The agent's findings are merged into the list. Any rejected agent finding is noted in one line with the reason.

**Verification:**
- [x] The API section of `review-m15.md` is complete; no source changes.

**Dependencies:** Task 1

**Files likely touched:**
- `tasks/review-m15.md`

**Estimated scope:** M

---

## Task 4: Review `Klasly.Worker`

**Description:** Check the Hangfire setup and the five tick steps against `CLAUDE.md` §3.5 and §27.8.

**Acceptance criteria:**
- [x] **Hangfire setup:**
  - SQL Server storage;
  - `WorkerTickJob` registered with `AddOrUpdate`, and the cron read from `Worker:Cron`;
  - `[DisableConcurrentExecution]` and `[AutomaticRetry(Attempts = 0)]`;
  - no web host and no dashboard;
  - the job only calls services, in the right order.
- [x] **The five steps:**
  - risk follows §27.8: Excused and Cancelled are excluded, and the weighted average is NULL until a grade is posted;
  - the thresholds are read from `SystemSettings`;
  - the DedupKey format is right, and alerts go only to guardians with `ReceivesAlerts = 1`;
  - Brevo: 10 s timeout, 2xx/permanent/transient handled correctly, backoff + jitter, `MaxEmailAttempts`;
  - the Brevo api-key comes from configuration only, and no guardian email or key is logged.

**Verification:**
- [x] The Worker section of `review-m15.md` is complete; no source changes.

**Dependencies:** Task 1

**Files likely touched:**
- `tasks/review-m15.md`

**Estimated scope:** S

---

## Task 5: Review `Klasly.Web`

**Description:** Check the MVC portals against `CLAUDE.md` §3.4: they hold no data and no rules, they are safe forms, and they show the API's errors.

**Acceptance criteria:**
- [x] **No data access:** no reference to Core or Infrastructure, no EF Core package, and no connection string in `Klasly.Web.csproj` or appsettings.
- [x] **Every POST and every role:**
  - every POST action has `[ValidateAntiForgeryToken]`;
  - every area controller has `[Authorize(Roles = …)]` (the M14 Q5 one-line check).
- [x] **The JWT cookie:**
  - HttpOnly + Secure + SameSite;
  - it is attached only through `BearerTokenHandler`;
  - logout clears it.
- [x] **Views:**
  - ViewModels and validation messages are used;
  - the API's error messages are shown through `ApiExceptionFilter` / `_Alerts`;
  - times are shown in Manila time;
  - no business rule is computed in a view or controller;
  - the QR page polls an MVC action, not the API.

**Verification:**
- [x] The Web section of `review-m15.md` is complete; no source changes.

**Dependencies:** Task 1

**Files likely touched:**
- `tasks/review-m15.md`

**Estimated scope:** M

---

## Task 6: Review Android + tests, and fill in the traceability tables

**Description:** Check the Android app against `CLAUDE.md` §3.6, check the test project for weak spots, and map every §27.9 requirement and every §27.8 scan check to the code that does it and to a test.

**Acceptance criteria:**
- [x] **Android:**
  - MVVM with `StateFlow`;
  - loading, empty and error states on every screen;
  - the API's scan message is shown;
  - the JWT is stored in EncryptedSharedPreferences;
  - cleartext traffic in the debug build only;
  - the `INTERNET` and `CAMERA` permissions are declared;
  - no hard-coded IP or key;
  - the `lintDebug` result from Task 1 is triaged.
- [x] **Tests:**
  - no test touches the `Klasly` database (the guard is in place);
  - no skipped or empty tests;
  - assertions check something real (not just "no exception").
- [x] **Traceability tables filled in:**
  - §27.9 requirements 1–10 → code + test;
  - scan checks 1–7 + the late cut-off → test.
  
  Every empty cell becomes a finding.

**Verification:**
- [x] The Android and Tests sections, plus both traceability tables, are complete; no source changes.

**Dependencies:** Task 1

**Files likely touched:**
- `tasks/review-m15.md`

**Estimated scope:** M

---

### Checkpoint A
- [x] `review-m15.md` covers all 6 areas. Every row has an id, a severity, `file:line` and a rule.
- [x] Review with the team (2026-11-11: "continue until all of the tasks are done", so the spec's severity table decided): every Medium finding is marked **fix** or **defer: reason**. Fix groups 8–11 are sized from the list.
- [x] `git status` shows no source changes since Task 1.

---

## Task 7: Serilog in the API and the Worker

> **Outcome:** `512493f`. The manual checks passed: no password, JWT or email in the API log; no guardian email or api-key in the Worker tick log.

**Description:** Replace the default logging provider with Serilog (console sink) in both hosts. This fixes F-01. Before any package is added, its name and version are stated, and the version is checked to be the latest stable that supports `net10.0`.

**Acceptance criteria:**
- [x] **API:** `Serilog.AspNetCore` is installed, and `Program.cs` sets up Serilog from configuration with a console sink. The existing `ILogger<T>` calls are not changed.
- [x] **Worker:** `Serilog.Extensions.Hosting` + `Serilog.Sinks.Console` (+ `Serilog.Settings.Configuration` if config-driven) are installed, and `Program.cs` sets up Serilog the same way.
- [x] **Configuration:** the minimum levels live in `appsettings.json` under `Serilog`, which replaces `Logging`. No request-body logging and no file sink.
- [x] **Startup:** the API still logs `Now listening on: …`, and the Worker logs each tick.

**Verification:**
- [x] `dotnet build`, `dotnet test` green; `dotnet list package` shows Microsoft 10.x and no vulnerable packages.
- [x] Manual check:
  1. run the API;
  2. log in as `aira.mendoza@klasly.test`;
  3. search the console output for `Klasly@123` and for `eyJ` (the start of a JWT). Neither may appear.
- [x] Manual check: run the Worker for one tick; the console output contains no guardian email and no `api-key`.

**Dependencies:** Checkpoint A

**Files likely touched:**
- `src/Klasly.API/Klasly.API.csproj`, `src/Klasly.API/Program.cs`, `src/Klasly.API/appsettings.json`
- `src/Klasly.Worker/Klasly.Worker.csproj`, `src/Klasly.Worker/Program.cs`, `src/Klasly.Worker/appsettings.json`

**Estimated scope:** M (6 small files)

---

## Task 8: Fix group: Core / Infrastructure / API

> **Outcome:** `cf32c74`: F-02 (deactivated accounts), F-03 (login timing), F-07 tests, F-20.

**Description:** Fix every Critical and High finding in these three projects, plus the Medium findings the team agreed to at Checkpoint A. Each Critical or High fix comes with a test that fails before the fix.

**Acceptance criteria:**
- [x] Every agreed finding in these areas is marked **fixed (`<commit>`)** in `review-m15.md`.
- [x] Each Critical/High fix has a regression test at the lowest level that proves it (rule → service → `ApiFactory` HTTP).
- [x] A schema change, a contract change, or a fix over ~30 lines was approved by the team before it was made.

**Verification:**
- [x] The new test fails on the old code and passes on the new.
- [x] `dotnet build` has no more warnings than the baseline; `dotnet test` green.

**Dependencies:** Checkpoint A (and Task 7 if a finding touches `Program.cs`)

**Files likely touched:** set from the findings list at Checkpoint A (≤ 5 per commit; split into 8a/8b if more)

**Estimated scope:** set at Checkpoint A

---

## Task 9: Fix group: Worker

> **Outcome:** nothing to fix. Every Worker item passed or is Low (see `review-m15.md`).

**Description:** Fix the agreed Worker findings, the same way as Task 8.

**Acceptance criteria:**
- [x] Every agreed Worker finding is marked **fixed (`<commit>`)**, with a test for each Critical/High one. Otherwise this task is marked "nothing to fix".

**Verification:**
- [x] `dotnet build`, `dotnet test` green; one Worker tick runs cleanly against LocalDB.

**Dependencies:** Checkpoint A

**Files likely touched:** set at Checkpoint A

**Estimated scope:** set at Checkpoint A

---

## Task 10: Fix group: Web

> **Outcome:** nothing to fix. The only Web finding (F-18) is Low, so no page changed and no browser check was needed.

**Description:** Fix the agreed MVC findings. A UI change is made only where it fixes a §3.4 rule break (Q5).

**Acceptance criteria:**
- [x] Every agreed Web finding is marked **fixed (`<commit>`)**. Otherwise this task is marked "nothing to fix".
- [x] Each changed page is checked in the browser preview, with a screenshot in the report.

**Verification:**
- [x] `dotnet build`, `dotnet test` green; the changed pages load with no console errors.

**Dependencies:** Checkpoint A

**Files likely touched:** set at Checkpoint A

**Estimated scope:** set at Checkpoint A

---

## Task 11: Fix group: Android and test gaps

> **Outcome:** `27a506b` (F-06 backup rules), `823d725` (F-08 admin rules, F-09 vertical slice).

**Description:** Fix the agreed Android findings, and add the tests the traceability tables show are missing.

**Acceptance criteria:**
- [x] Every agreed Android finding is marked **fixed (`<commit>`)**.
- [x] Every empty cell in the traceability tables is filled with a passing test, or with a finding that has an outcome.

**Verification:**
- [x] `./gradlew assembleDebug testDebugUnitTest` green; `dotnet test` green.

**Dependencies:** Checkpoint A

**Files likely touched:** set at Checkpoint A

**Estimated scope:** set at Checkpoint A

---

### Checkpoint B
- [x] `dotnet build` has no more warnings than the baseline; `dotnet test` and `./gradlew assembleDebug testDebugUnitTest` green.
- [x] `dotnet list package`: every Microsoft package is 10.x; `--vulnerable --include-transitive` shows none.
- [x] Every row in `review-m15.md` has an outcome.

---

## Task 12: Real-phone end-to-end run and close-out

**Description:** The team runs the whole flow once on a real phone over Wi-Fi or a hotspot, with no USB cable, and I guide each step. This closes the last M14 criterion and the M15 manual criterion.

**Acceptance criteria:**
- [ ] On the `lan` profile, with no cable, each step works:
  1. the teacher opens a session on MVC;
  2. a student scans on the phone;
  3. the teacher's live list shows the scan;
  4. the Worker closes the session and marks the non-scanners Absent;
  5. the teacher posts a grade and the phone gets the push;
  6. the parent overview shows the attendance and the grade.
- [ ] Every M15 success criterion in `SPEC.md` is ticked. The M14 real-phone criterion in the M14 spec (git history) is noted as done in the report.

**Verification:**
- [ ] Manual: a screenshot of each step in the report.
- [ ] The final `dotnet build` / `dotnet test` / `assembleDebug` results are written in the report.

**Dependencies:** Checkpoint B (needs the team, a phone and the PC on the same network)

**Files likely touched:**
- `SPEC.md` (tick the criteria)
- `tasks/plan-m15.md`, `tasks/todo-m15.md` (tick)
- `docs/reports/<…>/`

**Estimated scope:** S

---

### Checkpoint C (complete)
- [ ] All spec success criteria are ticked, with the evidence in the reports.
