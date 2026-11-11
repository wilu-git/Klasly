# How to use — Milestone 13 Task 8 (Child overview for parents in the app)

Parents can now see their children's attendance, grades and at-risk status in the Klasly app.

## Prerequisites
- SQL Server LocalDB, the Web API running with `dotnet run --project src/Klasly.API --launch-profile http` (not `https`: the emulator can't follow its redirect), and optionally the Worker (for up-to-date at-risk badges).
- The debug app on the emulator: in `android/KlaslyApp`, `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew installDebug`.

## Step by step
1. Open Klasly and log in as a test parent: `marites.mendoza@klasly.test` / `Klasly@123` (fictional).
2. The first child's overview opens. With more than one child, tap a name chip at the top to switch.
3. Each card is one class:
   - **badge:** At risk (red) or On track (green), with "This is an early-warning indicator for review, not a final grade";
   - **counts:** Present · Late · Absent · Excused;
   - **Absence rate** and **Weighted average**;
   - tap **Show attendance records** or **Show grades** for details.
4. Pull down to refresh. **Log out** is at the top right.

## Example
> **Aira Mendoza** · 2026-00001 · 1st Sem 2026-27 — chips: [Aira Mendoza] [Bryan Lacsamana]
> **CPE 401 · Embedded Systems Design** — M5 · Mark Delos Santos — **At risk**
> Present · Late · Absent · Excused: 0 · 0 · 2 · 0 · Absence rate 100% · Weighted average: No grades yet

## Troubleshooting
- **"No children are linked to your account yet"**: the school admin links parents to children on Admin › Accounts in the web portal.
- **"Couldn't load your records"**: the API isn't reachable. Check it's running, then tap **Try again**.
- **No badge on a class**: the Worker hasn't checked that class yet. Start the Worker and pull to refresh after a few minutes.
