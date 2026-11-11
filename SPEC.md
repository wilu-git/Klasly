# Spec: Milestone 15 — Review of the whole solution; fix findings

> Status: **Approved** (2026-11-11). Open questions 1–5 answered as recommended: add Serilog to the API and the Worker (console sink only); I review inline, plus one `security-auditor` pass over `Klasly.API`; keep the deprecated Android APIs (logged as Low); the real-phone run is the last task; no full `/impeccable` pass.
> Plan: `tasks/plan-m15.md` + `tasks/todo-m15.md`.
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§2–21 coding rules, §25 final checklist, §27.5 security, §27.7 schema conventions, §27.8 business rules, §27.9 functional requirements) → `CLAUDE.md` (§3 per-project rules, §4 rules, §4.1 .NET 10, §6 Milestone 15) → this spec.
> Milestone 14's spec is in git history (commit `867d40a`) and in `docs/reports/2026-11-11-1430-milestone-14-hardening/`.

## What exists today

- **All Core features are built** (Milestones 1–14): the 4 role logins, admin CRUD, rotating QR + scan, auto-absent, corrections, assessments + grades + FCM push, risk flags, the Brevo outbox, the dashboards, and the admin Failed emails list.
- **Tests:** `tests/Klasly.Tests` has 107 `[Fact]`/`[Theory]` methods. They cover rules, services and in-memory HTTP: the role sweep, ownership, the 7 scan checks, and the exact 20% / 75 boundaries. The Android unit tests (11) cover `DisplayText` and the view models.
- **Packages:** every Microsoft package is 10.0.14. Third-party packages: Hangfire 1.8.25, Swashbuckle.AspNetCore.SwaggerUI 10.3.1, QRCoder 1.8.1.
- **Known gaps already found (not yet fixed):**
  1. **Serilog is not installed.** `CLAUDE.md` §3.3 and guide §16 require it. The API and the Worker use the default `ILogger` console logging. M14 deferred this to the M15 review.
  2. **The M14 real-phone check is still open:** log in and scan over Wi-Fi with no USB cable, then take screenshots. Only the team can do it, with a phone.
  3. **9 `shortcut:` comments** record limits we accepted on purpose (`/ponytail-debt` lists them). Three of them are deprecated Android APIs: `security-crypto`, `getToken` and `onNewToken`.
- Guide §27.3 still says the Worker is a `BackgroundService`. `CLAUDE.md` §0 overrides that with Hangfire, so this is **not** a finding.

## Assumptions (correct these before we start)

1. **The review covers the whole repo, one area at a time.** Each pass ends with a findings list before anything gets fixed:
   - `Klasly.Core` + `Klasly.Infrastructure` (entities, Fluent API, migrations, repositories)
   - `Klasly.API`
   - `Klasly.Worker`
   - `Klasly.Web`
   - `android/KlaslyApp`
   - `tests/Klasly.Tests`
2. **What each pass checks against** (a fixed checklist, so the review is repeatable):
   - **Rules:** guide §25 Final AI Checklist; `CLAUDE.md` §3.x for that project, plus §4 and §4.1 (.NET 10 code style, no `[Obsolete]` APIs).
   - **Security:** §27.5. Server-side ownership on every endpoint, hashed passwords, no secrets in tracked files (the git history too), CORS off, `[ValidateAntiForgeryToken]` on every MVC POST, an HttpOnly + Secure + SameSite cookie, no sensitive data in logs (§16), cleartext traffic in the Android debug build only.
   - **Business rules:** every row of the §27.8 tables and every §27.9 functional requirement is traced to the code that does it and to a test. A requirement with no test is a finding.
   - **Schema:** run `dotnet ef migrations script` and compare it with `docs/database/Klasly_Schema.sql` (§27.7: CHECKs, unique rules, the filtered index, `NoAction`, UTC defaults, indexes). The `HangFire` schema is ignored.
   - **Five axes:** correctness, readability, architecture (layer flow §2), security, performance (N+1 queries, `AsNoTracking` on reads, async + `CancellationToken` all the way down).
   - **Bloat:** dead code, unused files and packages, duplicates.
3. **How findings are graded and handled:**

   | Severity | Meaning | Action in M15 |
   |---|---|---|
   | **Critical** | Security hole, data loss, or a broken Core feature | Fix, plus a test that fails before the fix |
   | **High** | Breaks a guide/`CLAUDE.md` rule, or a wrong status code or message | Fix |
   | **Medium** | A real problem that does not block the demo | Fix if the change is small (≤ ~30 lines and needs no new package); otherwise log it |
   | **Low / nit** | Style, naming, a comment | Log only |

   Every finding gets an id (`F-01`, …), a severity, `file:line`, the rule it breaks, and an outcome: **fixed** (with the commit), **deferred** (with a reason), or **won't fix** (with a reason).
4. **Fixes stay small and are grouped by area,** with one commit per group, for example `fix(api): F-03, F-07 …`. No refactor unless a finding needs one. Moved code keeps its validation and error handling.
5. **The `shortcut:` comments are reviewed, not removed by default.** Each one is either still acceptable for the demo (keep it) or becomes a finding.
6. **No new features.** Nice-to-Have and Optional features from §27.2 stay out, even if the review shows where they would fit.
7. **The review is read-only first.** Pass 1 writes only the findings list (`tasks/review-m15.md`). Code changes start only after the team has seen the list and agreed which Medium findings to fix.

## Objective

Before the demo and submission, find and fix anything that breaks the guide's rules, the security rules or the business rules, and leave a written record of what was checked and what was decided.

### User stories
- As the **team**, we have one findings list that shows every rule was checked, what was fixed, and why anything left open was left open. We can show it to our instructor.
- As a **teacher, student, parent or admin**, the app behaves the way §27.8 and §27.9 say, and no role can see another role's data.
- As **the next developer**, logs are structured (Serilog) and contain no secrets or personal data, so problems can be traced *(if Q1 = yes)*.

## Tech stack

- No change: .NET 10 / ASP.NET Core 10 / EF Core 10, Hangfire 1.8.25, xUnit v3, Kotlin + Jetpack Compose.
- **Possible new packages (Q1):** `Serilog.AspNetCore` (API) and `Serilog.Extensions.Hosting` + `Serilog.Sinks.Console` (Worker), at the latest stable versions that support `net10.0`. Version numbers will be checked and stated before they are added.

## Commands

```bash
dotnet build Klasly.slnx -warnaserror:false
dotnet test Klasly.slnx
dotnet list package
dotnet list package --vulnerable --include-transitive
dotnet list package --deprecated
dotnet ef migrations script -p src/Klasly.Infrastructure -s src/Klasly.API -o <scratch>/klasly-model.sql
git log -p --all -S "Jwt:Key" -- . ":!docs"
graphify update .
```

Android (from `android/KlaslyApp`):

```bash
./gradlew assembleDebug testDebugUnitTest lintDebug
```

## Project structure (what this milestone touches)

```
SPEC.md                      ← this file
tasks/plan-m15.md            ← after approval
tasks/todo-m15.md            ← after approval
tasks/review-m15.md          ← NEW: findings list (id, severity, file:line, rule, outcome)
src/**, android/**, tests/** ← only the lines a finding points to
docs/reports/<…>/            ← per CLAUDE.md §5, one per fix group
```

## Code style

A finding row in `tasks/review-m15.md`:

```markdown
| ID   | Sev  | Where                                   | Rule              | Finding                                    | Outcome            |
|------|------|-----------------------------------------|-------------------|--------------------------------------------|--------------------|
| F-01 | High | src/Klasly.API/Program.cs:1             | CLAUDE.md §3.3    | Serilog not installed; default logging     | fixed (`abc1234`)  |
| F-02 | Low  | src/Klasly.API/Push/PushNotificationSender.cs:8 | shortcut   | Stale FCM token not cleared                | won't fix: demo scale |
```

Fix commits follow the existing style (`fix(api): …`, `fix(web): …`, `fix(android): …`, `test: …`). A fixed Critical finding gets a test named after the rule it protects, the same way the existing tests are named.

## Testing strategy

- **Baseline first:** record the current `dotnet build` warning count, the `dotnet test` result and the `./gradlew assembleDebug testDebugUnitTest` result before any fix. Every fix group must leave all of them the same or better.
- **Critical and High fixes** get a test that fails before the fix and passes after it, at the lowest level that proves it (rule → service → HTTP).
- **Traceability table** in `tasks/review-m15.md`: each §27.9 requirement (1–10) and each §27.8 scan check, mapped to its test. A gap is either filled with a test or logged as a finding.
- **Manual end-to-end run (if Q4 = yes):** the M14 real-phone check, done by the team with me guiding:
  1. the teacher opens a session on MVC;
  2. a student scans on a real phone over Wi-Fi;
  3. the scan shows in the teacher's live list;
  4. the Worker closes the session and marks the non-scanners Absent;
  5. the teacher posts a grade and the phone gets the push;
  6. the parent overview shows the result.

  Screenshots go in the report.

## Boundaries

- **Always:**
  - findings list first, fixes after;
  - one fix group per commit;
  - run `dotnet build` + `dotnet test` (and `assembleDebug` for Android) after every fix group, and report the results honestly;
  - assert the API's messages word for word;
  - run `graphify update .` after code changes.
- **Ask first:**
  - adding any package (Serilog, Q1);
  - any schema change or new migration;
  - replacing a deprecated Android library (Q3);
  - fixing a Medium finding that is more than ~30 lines;
  - any change to a public API contract (route, DTO shape, status code) that MVC or Android depends on.
- **Never:**
  - commit secrets or rewrite git history (if a secret is found in history, report it and stop);
  - weaken a check or delete or skip a test to get green;
  - add Nice-to-Have or Optional features;
  - turn CORS on;
  - change Windows Firewall or other system security settings.

## Success criteria

- [ ] `tasks/review-m15.md` exists and covers all 6 areas, each checked against the full checklist in Assumption 2.
- [ ] Every §27.9 requirement and every §27.8 scan check is mapped to code and a test, or logged as a finding.
- [ ] `dotnet ef migrations script` matches `Klasly_Schema.sql` on every §27.7 convention, or each difference is a finding with an outcome.
- [ ] No secret is found in tracked files or git history (`Jwt:Key`, `Qr:SigningKey`, Brevo api-key, FCM JSON, connection strings with passwords).
- [ ] Every Critical and High finding is **fixed** and has a test. Every Medium finding is fixed or has a written reason. Every Low finding is logged.
- [ ] *(Q1 = yes)* Serilog writes structured logs in the API and the Worker. A test or a log sample shows that no password, JWT, key or guardian email is logged.
- [ ] `dotnet build` has no more warnings than the baseline; `dotnet test` and `./gradlew assembleDebug testDebugUnitTest` are green; `dotnet list package` shows no Microsoft package below 10.x and `--vulnerable` shows none.
- [ ] *(Q4 = yes)* The end-to-end run on a real phone passes, with screenshots in the report.

## Open questions (my recommendation first)

1. **Install Serilog now?** `CLAUDE.md` §3.3 and guide §16 both name it, and it is the one known rule break. It is 2–3 packages plus about 10 lines per `Program.cs`, and the existing `ILogger` calls stay the same. **Recommend: yes, in the API and the Worker. Console sink only, no file or seq sink.**
2. **Who reviews?** (a) I review each area inline, in order. (b) I run the `agent-skills:code-reviewer` and `agent-skills:security-auditor` agents in parallel per area, then check and merge what they find. (b) finds more, but costs more and gives more false positives to sort through. **Recommend: (a), plus one `security-auditor` pass over `Klasly.API` only, because that is the security boundary.**
3. **Deprecated Android APIs** (`security-crypto`, FCM `getToken` / `onNewToken`)? They still work on SDK 26+ and with firebase-messaging 26. Replacing them now risks breaking login and push right before the demo. **Recommend: keep them, logged as Low (won't fix before the demo).**
4. **Include the M14 real-phone check as the final M15 step?** It is the last unticked M14 criterion and the best rehearsal for the demo. **Recommend: yes, as the last task, after all fixes are in.**
5. **Run `/impeccable` again on all MVC views and Compose screens?** Each milestone already ran it on its own views. **Recommend: no full UI pass. Only fix UI findings that break §3.4 or §3.6 rules, such as a missing error, empty or loading state, or a missing validation message.**
