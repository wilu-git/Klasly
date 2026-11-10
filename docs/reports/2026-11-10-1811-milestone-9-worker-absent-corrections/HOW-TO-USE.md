# How to use: Milestone 9 — automatic Absent marking and attendance corrections

## Prerequisites

- SQL Server LocalDB with the `Klasly` database.
- The Web API, the MVC website and the Worker running. For a demo, make the Worker tick every minute in PowerShell:

  ```powershell
  $env:Worker__Cron = "* * * * *"; dotnet run --project src/Klasly.Worker
  ```

- Teacher login (fictional test data): `rosa.villanueva@klasly.test`, password `Klasly@123`.

## Step by step

1. Log in as the teacher and start a session from **My classes → ELECT3 – BSCpE 3-A**. Students scan the QR with the Klasly app.
2. When the end time passes (or you press **End session**), the Worker's next tick:
   - marks everyone who didn't scan as **Absent** (Source **Auto**);
   - sets the session to **Finalized**.
3. Reload the session page:
   - the grey note says the session has ended;
   - the summary shows how many were Present, Late, Absent and Excused;
   - the full roster fills the page.
4. To fix a result, choose a new status in the **Correct** column and press **Save**:
   - you get "Attendance updated for …" and the row shows Source **Corrected**;
   - if you press Save without changing anything, the page says "No change: … is already …".
5. **On a phone:** the roster shows Name, Status (with the source under it) and Correct, so you don't have to scroll sideways.

A session that was never opened becomes **Cancelled** after its end time. Its page says "This session was never opened, so no attendance was taken."

## Example (screen)

A finalized session on a laptop:
- the title has a grey **Finalized** badge;
- the note "This session has ended… To fix a result, choose a new status in the Correct column and press Save.";
- the summary "1 of 6 checked in · 0 Present · 1 Late · 4 Absent · 1 Excused";
- a table with Student no., Name, Status, Scanned (Manila), Source and Correct (a dropdown and a Save button on each row).

## Troubleshooting

| Problem | Fix |
|---|---|
| No dropdowns on the page | Corrections open only after the session has ended (Closed or Finalized). Wait for the end time and one Worker tick, or press **End session**. |
| The session still says Open after its end time | The Worker isn't running. Start it (see Prerequisites). |
| "Attendance was just updated. Reload and try again." | Someone else, or the Worker, saved this student at the same moment. Reload the page and choose again. |
