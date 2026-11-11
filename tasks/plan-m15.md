# Implementation Plan: Milestone 15 — Review of the whole solution; fix findings

> Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-11, open questions 1–5 as recommended). Task details, acceptance criteria and verification steps are in [`todo-m15.md`](./todo-m15.md).
> Milestone 14 is in `plan-m14.md` / `todo-m14.md`. Its only open item is the real-phone check, which moves here as Task 12. `tasks/plan.md` / `todo.md` belong to Milestone 8 and are left untouched.

## Overview

Every Core feature is built and tested (Milestones 1–14). M15 reviews the whole solution in a fixed order against a fixed checklist, writes every finding into one list (`tasks/review-m15.md`), and then fixes the findings in small groups, one area at a time. The milestone ends with a real phone running the whole flow once, end to end, as a rehearsal for the demo.

**Decisions (spec open questions, 2026-11-11):**
- Q1: add Serilog to the API and the Worker. Console sink only.
- Q2: I review each area inline, plus one `agent-skills:security-auditor` pass over `Klasly.API`.
- Q3: keep the deprecated Android APIs (`security-crypto`, FCM `getToken` / `onNewToken`). They are logged as Low, won't fix before the demo.
- Q4: the M14 real-phone check becomes the last task, extended to the full flow.
- Q5: no full `/impeccable` pass. A UI finding is fixed only when it breaks a `CLAUDE.md` §3.4 or §3.6 rule.

## Architecture decisions

- **Review first, fix later.** Tasks 2–6 are read-only and only add rows to `tasks/review-m15.md`. No source file changes until Checkpoint A, where the team reads the list and decides which Medium findings to fix (spec Assumption 7). If someone fixes things while still reviewing, the review stops covering everything.
- **One checklist for every area** (spec Assumption 2), copied into the top of `review-m15.md` so each area is checked against the same items:
  - guide §25;
  - the `CLAUDE.md` §3.x rules for that project, plus §4 and §4.1;
  - the §27.5 security rules;
  - §27.8 and §27.9, each traced to code and a test;
  - §27.7 schema;
  - the five axes (correctness, readability, architecture, security, performance);
  - bloat.
- **Finding ids are global and stable:** `F-01`, `F-02`, … in the order they are found. They are never renumbered. Fix commits name their ids (`fix(api): F-04, F-09 …`), so every outcome points to a commit.
- **Traceability lives in the findings file.** It has one table for §27.9 requirements 1–10 and one for the 7 §27.8 scan checks plus the late cut-off, each listing the code and the test. An empty cell becomes a finding.
- **Serilog is its own task (Task 7), not part of a fix group.** It adds packages and touches both `Program.cs` files, so it has to be reviewed on its own. The existing `ILogger<T>` calls stay as they are. Serilog only replaces the provider underneath them.
- **Fix groups follow the projects** (Tasks 8–11), so each one touches one area and can be built and tested on its own. A fix group with no agreed findings is marked "nothing to fix" and skipped.
- **Baseline before everything.** Task 1 records the warnings, test results and package state, so "no worse than baseline" can actually be checked.

## Reuse (don't rebuild)

- **Tests:**
  - `DatabaseFixture` (`Klasly_Tests`);
  - `ApiFactory` + `LoginAsync` for HTTP-level regression tests;
  - `TestData.NewClassAsync`.
- **The `shortcut:` comments** (9 today, listed by `/ponytail-debt`) are the starting point for the "accepted limits" rows.
- **`/ponytail-review` or `agent-skills:code-review-and-quality`** is used as the five-axis lens inside each pass. It is not a separate deliverable.
- **Reports:** the `CLAUDE.md` §5 folder format, one folder per fix group.

## Dependency graph

```
T1 Baseline ──┬─ T2 Review: Core + Infrastructure (+ schema compare)
              ├─ T3 Review: API (+ security-auditor)
              ├─ T4 Review: Worker
              ├─ T5 Review: Web
              └─ T6 Review: Android + tests (+ traceability tables)
                       │
                Checkpoint A  (team agrees on which Medium findings to fix)
                       │
              ┌────────┼──────────┬──────────┬──────────┐
              T7       T8         T9         T10        T11
           Serilog  Fix: Core/   Fix:       Fix:       Fix: Android
           (API +   Infra + API  Worker     Web        + test gaps
           Worker)
              └────────┴──────────┴──────────┴──────────┘
                       │
                Checkpoint B  (all green vs baseline)
                       │
              T12 Real-phone end-to-end run + close-out
```

T7 and T8 both touch `src/Klasly.API/Program.cs` only if a finding points there. If one does, T7 goes first.

## Task list

### Phase 1: Baseline
- [ ] Task 1: Baseline (build warnings, tests, Android, packages, secrets scan, schema script) + `review-m15.md` skeleton — S

### Phase 2: Review (read-only)
- [ ] Task 2: Review `Klasly.Core` + `Klasly.Infrastructure`, incl. the §27.7 schema comparison — M
- [ ] Task 3: Review `Klasly.API` (inline + one `security-auditor` pass) — M
- [ ] Task 4: Review `Klasly.Worker` (Hangfire job, risk, outbox, Brevo) — S
- [ ] Task 5: Review `Klasly.Web` (no DB access, anti-forgery, cookie, error display) — M
- [ ] Task 6: Review `android/KlaslyApp` + `tests/Klasly.Tests`, and fill in the traceability tables — M

### Checkpoint A
- [ ] `review-m15.md` covers all 6 areas, every row has a severity, and the traceability tables are filled in.
- [ ] Review with the team. Every Medium finding is marked "fix" or "defer: reason". No code has changed yet.

### Phase 3: Fix
- [ ] Task 7: Serilog in the API and the Worker (console sink), plus a check that no secrets or personal data are logged — S
- [ ] Task 8: Fix group: Core / Infrastructure / API findings (Critical + High + agreed Medium) — size set at Checkpoint A
- [ ] Task 9: Fix group: Worker findings — size set at Checkpoint A
- [ ] Task 10: Fix group: Web findings — size set at Checkpoint A
- [ ] Task 11: Fix group: Android findings + missing tests from the traceability tables — size set at Checkpoint A

### Checkpoint B
- [ ] `dotnet build` no more warnings than the baseline; `dotnet test` and `./gradlew assembleDebug testDebugUnitTest` green; `dotnet list package` shows Microsoft 10.x and no vulnerable packages.
- [ ] Every finding in `review-m15.md` has an outcome (fixed + commit / deferred + reason / won't fix + reason).

### Phase 4: Rehearse and close
- [ ] Task 12: Real-phone end-to-end run (team, with me guiding) + tick the M15 and M14 criteria — S

### Checkpoint C (complete)
- [ ] Every spec success criterion is ticked, with screenshots in the report.

## Parallelization

- Tasks 2–6 are read-only and independent of each other. Per Q2 they still run one after another, inline, so the findings list keeps a single voice and ids aren't duplicated. The `security-auditor` agent in Task 3 runs in the background while I review the API by hand, and I check and merge its results.
- Tasks 9, 10 and 11 touch different projects and could run in parallel. They are kept in order because each is small and every one ends with the same `dotnet test` run.
- Task 12 needs the team and a phone. It runs after Checkpoint B, so the rehearsal uses the final code.

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| The review turns up a Critical issue that needs a schema change | High | It is "ask first" in the spec. Stop, show the migration diff, and only then run `dotnet ef migrations add`. |
| A secret turns up in git history | High | Report it and stop. Never rewrite history. The team rotates the key; the fix is a new key, not a force-push. |
| The findings list grows too long to fix before the demo | Med | The severity table decides what gets fixed: Critical and High always, Medium only where the team agreed at Checkpoint A, Low is logged only. |
| Serilog changes the log format and breaks someone's habit (e.g. the "Now listening on" line) | Low | Use a console sink with the default template. The ASP.NET Core startup lines still show. |
| Serilog logs request bodies or headers (passwords, JWTs) | Med | No request-logging middleware that captures bodies. Task 7 includes a check: log in with a known password and grep the console output for the password and the token. |
| A "fix" changes a status code or message that MVC or Android depends on | Med | Contract changes are "ask first". The HTTP tests assert the messages word for word, so any drift shows up as a failing test. |
| The security agent reports false positives | Low | Every agent finding is checked against the code before it becomes a row. Rejected ones are noted in one line. |
| Campus Wi-Fi blocks phone-to-PC traffic during Task 12 | High | Use the phone's hotspot (as in M14 Task 1). Check the path first by opening `/api/auth/me` in the phone's browser. |

## Out of scope

- New features (Nice-to-Have and Optional in §27.2).
- Replacing the deprecated Android libraries (Q3).
- A full `/impeccable` UI pass (Q5).
- File or Seq logging sinks, request logging, any cloud host or tunnel.
- Rewriting git history.

## Open questions

None. All five spec questions are answered as recommended.
