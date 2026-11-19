# Demo data and deploy prep (Tasks 1–4 of `tasks/plan-demo.md`)

## Summary
Asked: do everything needed to get Klasly ready to deploy, with demo data.
Done:
- `deploy/demo-data.sql`: four weeks of finished sessions, attendance and grades for the seeded classes, with a test that checks the exact at-risk mix;
- Linux self-contained builds of the API, Web and Worker, plus the migration bundle, in `publish/` (git-ignored);
- the demo checklist updated;
- an SSH deploy key created.
The VPS deploy itself (Task 5) waits until the user installs the key and gives the login user and domain.

## Projects touched
EF CORE / MSSQL (a data script, no schema change), CONSOLE WORKER (its risk step is exercised by the test), deploy files.

## Files changed
| Path | Change | Why |
|---|---|---|
| `SPEC.md` | modified | spec for the demo deploy and demo data; criteria ticked |
| `tasks/plan-demo.md` | added | the 5-task plan |
| `deploy/demo-data.sql` | added | one-batch T-SQL demo history, safe to run twice |
| `tests/Klasly.Tests/DemoDataTests.cs` | added | fresh DB → migrate → script → Worker risk step → assert the spec table; a second run adds nothing |
| `docs/reports/2026-11-20-0257-demo-readiness/HOW-TO-USE.md` | modified | demo-data step, Worker started last, Contact email, Dennis's live flip |

## Key code
- The window is Manila dates from 30 to 3 days ago, which always gives 4 of each weekday: 12 MWF sessions for ELECT3 and 8 TTh sessions for IT302. Nothing falls within the Worker's 48 h absence-alert lookback.
```sql
IF EXISTS (SELECT 1 FROM dbo.ClassSessions) OR EXISTS (SELECT 1 FROM dbo.Assessments)
BEGIN
    PRINT 'Demo data skipped: ...';
    RETURN;
END;
```
- Records look like the real ones: Present/Late as `Scan` with `ScannedAt` (Late at +22 min, after the 15-minute cut-off), Absent as `Auto`.
- The test runs the real `RiskCalculator.RecomputeAsync` on a database of its own, `Klasly_DemoData_Tests`, so the other tests' rows can't change the numbers.

| Class | Student | Absence | Average | At risk |
|---|---|---|---|---|
| ELECT3 | Bryan | 0.3333 | 79.50 | yes |
| ELECT3 | Carla | 0.0833 | 65.75 | yes |
| ELECT3 | Dennis | 0.1667 | 79.25 | no, flips at 3/13 in the live demo |
| others | — | ≤ 0.125 | ≥ 76.80 | no |

## Database changes
None to the schema. The script inserts rows only: 20 `ClassSessions`, 96 `AttendanceRecords`, 5 `Assessments` and 24 `Grades`.

## How it was verified
- RED: the new test failed with "Could not find file deploy/demo-data.sql".
- GREEN: `dotnet build Klasly.slnx` with 0 warnings and 0 errors, and `dotnet test Klasly.slnx` 163/163 passed.
- `dotnet publish -r linux-x64 --self-contained` for all three apps and `dotnet ef migrations bundle -r linux-x64`: all four outputs start with the ELF header (`\177ELF`), 638 MB in total.
- `graphify update .` run.

## Known limits / TODO
- **Task 5 is not done:** nothing is deployed yet. It needs the key on the VPS, the login user and the domain.
- Not run against SQL Server 2022 on Linux. It uses only features that exist there too (`HASHBYTES`, `NEWID`, table variables).
- The upload is 638 MB. Compress it with tar + gzip before `scp`.
- No APK: this PC has no JDK or Android SDK.
