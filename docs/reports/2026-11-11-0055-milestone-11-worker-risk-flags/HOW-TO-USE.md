# How to use — Worker risk flags (Milestone 11)

The Worker now works out, every few minutes, whether each student is **at-risk** in each class. A student is at-risk when they missed 20% or more of the finished sessions, or when their weighted average is below 75. There is no screen for it yet (the dashboards come in Milestone 13); you can see the results in the database.

## Prerequisites
- SQL Server LocalDB with the `Klasly` database (`dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`).
- Some data to judge: finalized sessions (open a session, let it end, let the Worker finalize it) and/or posted grades.
- Nothing else. The API, MVC site and emulator are not needed for this step.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Start the Worker. The default runs every 5 minutes; for a quick try, run it every minute:
   ```
   dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"
   ```
3. Watch for the log line after each tick:
   ```
   Tick 2026-11-10 16:52:14Z: risk for 10 enrollments, 10 new snapshots, 10 at-risk
   ```
   - The first tick writes a row for every active student in every class of the active term.
   - Later ticks write a row **only when something changed**. "0 new snapshots" is normal.
4. Look at the results:
   ```
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d Klasly -Q "SELECT TOP 20 * FROM RiskFlags ORDER BY ComputedAt DESC"
   ```
5. To change the rules, edit `SystemSettings` (`AbsenceThreshold`, e.g. `0.25`; `PassingGrade`, e.g. `70`). The next tick uses the new values.
6. Press **Ctrl+C** to stop the Worker.

## Example
| EnrollmentId | AbsenceRate | WeightedAverage | IsAtRisk | Meaning |
| --- | --- | --- | --- | --- |
| 1 | 0.7500 | 90.00 | 1 | missed 12 of 16 sessions; good grades, still flagged |
| 4 | 1.0000 | 65.00 | 1 | missed everything and failing |
| 7 | 1.0000 | NULL | 1 | no grades yet; flagged on attendance alone |

Excused absences and cancelled sessions are not counted. A student who enrolled late is judged only on the sessions they were enrolled for.

## Troubleshooting
- **The log says `Setting 'AbsenceThreshold' is missing or invalid: it must be a fraction above 0 and up to 1, e.g. 0.20.`** (or the same for `PassingGrade`, which must be 1–100): the `SystemSettings` row was deleted, holds text, or was typed in the wrong scale (`20` instead of `0.20`, `0.75` instead of `75`). Put back a plain number with a dot for decimals. Nothing is written until it is fixed.
- **"risk for 0 enrollments"**: there is no active term, or the classes have no Active enrollments. Check `AcademicTerms.IsActive`.
- **A student missed a class but no new snapshot appeared**: only **Finalized** sessions count. A session is finalized by the first tick after its end time, and the risk step runs right after that in the same tick. If the session is still Open or Closed, wait for the next tick. An Excused record never counts.
