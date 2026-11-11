# How to use — Milestone 13 Task 7 (Student tabs in the app)

Students can now see their attendance and grades in the Klasly app, not just scan.

## Prerequisites
- SQL Server LocalDB, and the Web API running (`dotnet run --project src/Klasly.API --launch-profile https`).
- The Android emulator (or a phone) with the debug app installed: in `android/KlaslyApp`, `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew installDebug`.

## Step by step
1. Open Klasly and log in as a test student: `aira.mendoza@klasly.test` / `Klasly@123` (fictional).
2. The app opens on **Scan**, as before.
3. Tap **Attendance** at the bottom. Each card is one class: the counts, the absence rate and the list of sessions (absences in red).
4. Tap **Grades**: each assessment with your score (or "Not graded yet") and your weighted average.
5. Pull down on either tab to refresh.

## Example
**My attendance** · Aira Mendoza · 1st Sem 2026-27
> **ELECT3 · Mobile Application Development** — BSCpE 3-A · Rosa Villanueva
> Present · Late · Absent · Excused: 4 · 1 · 12 · 1 · Absence rate: 75%
> Nov 10, 9:00 AM — **Absent**

## Troubleshooting
- **"Couldn't load your records — Can't reach Klasly"**: the API isn't running, or the emulator can't reach `10.0.2.2`. Start the API and tap **Try again**.
- **"No classes this term"**: the student has no active enrollments in the active term. The admin enrolls students on the Classes page.
- **Sent back to the login screen**: the login expired. Log in again.
