# How to use: automatic Absent marking and attendance corrections

## Prerequisites

- SQL Server LocalDB with the `Klasly` database (migrated and seeded).
- The Web API running (`dotnet run --project src/Klasly.API --launch-profile https`).
- The MVC website running (`dotnet run --project src/Klasly.Web --launch-profile https`).
- The Worker running. For a quick demo, make it tick every minute in PowerShell:

  ```powershell
  $env:Worker__Cron = "* * * * *"; dotnet run --project src/Klasly.Worker
  ```

  Without the setting, it ticks every 5 minutes.
- Test logins (fictional data): teacher `rosa.villanueva@klasly.test`, password `Klasly@123`.

## Step by step

1. Log in to the website as the teacher, open **My classes → ELECT3 – BSCpE 3-A**, and start a session that ends in a few minutes.
2. Let a student scan the QR with the Klasly app (or skip this).
3. Wait until the end time has passed and the Worker has ticked once (up to 1 minute with the demo setting).
4. Reload the session page. The badge says **Finalized**:
   - students who scanned show **Present** or **Late** with Source **Scan**;
   - everyone else shows **Absent** with Source **Auto** (marked by Klasly).
5. To fix a result, pick a new status in the **Correct** column and press **Save**. Keyboard: Tab to the dropdown, arrow keys to choose, Tab to Save, Enter.
6. A green message says "Attendance updated for …". The row now shows the new status with Source **Corrected**.

Other cases:
- **Ending a session early:** press **End session** on the page. On the next tick the absentees are marked and the session becomes Finalized.
- **A session that was never opened:** once its end time passes, it becomes **Cancelled**. Its page says "This session was never opened, so no attendance was taken."

## Example

API request (teacher's JWT in `Authorization: Bearer …`):

```http
PUT /api/sessions/1014/attendance/1
Content-Type: application/json

{ "status": "Excused" }
```

Response `200 OK`:

```json
{ "studentId": 1, "studentNumber": "2026-00001", "fullName": "Aira Mendoza",
  "status": "Excused", "scannedAtUtc": null, "source": "Manual" }
```

**Screen:** the session page has the roster table with columns Student no., Name, Status (coloured badge), Scanned (Manila), Source, and Correct (dropdown + Save, only after the session ended).

## Running the tests

```bash
dotnet test --solution Klasly.slnx
```

The tests create their own `Klasly_Tests` database in LocalDB. They never touch `Klasly`.

## Troubleshooting

| Problem | Fix |
|---|---|
| Sessions never become Finalized | The Worker isn't running, or it ticks every 5 minutes. Check its console for `Tick … closed …, finalized …`. |
| "Attendance can be corrected only after the session has ended." | The session is still open. Press **End session**, or wait for its end time and one Worker tick. |
| `dotnet test` says "Testing with VSTest target is no longer supported" | Run it from the solution folder, so `global.json` (with the `test.runner` setting) is used, and use `dotnet test --solution Klasly.slnx`. |
