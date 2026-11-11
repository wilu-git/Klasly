# How to use — Milestone 15 (review and fixes)

Most of this milestone is under the hood. Users notice three changes:

1. **When the admin deactivates an account, it stops working right away.** Before, a teacher or parent who was already logged in could keep using Klasly for up to an hour.
2. **The API and the Worker log through Serilog.** Console lines now look like `[13:55:04 INF] Tick …`.
3. **The Android app no longer puts its login in phone backups.** After restoring a phone, the student just logs in again.

## Prerequisites

- SQL Server LocalDB with the `Klasly` database (`dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`).
- User-secrets set as before: `Jwt:Key` and `Qr:SigningKey` for the API, `Brevo:ApiKey` for the Worker.
- For the real-phone run: an Android phone, and the PC on the phone's hotspot.

## Step by step

### A. See that deactivation works at once

1. Start the API: `dotnet run --project src/Klasly.API --launch-profile https`. Then start MVC: `dotnet run --project src/Klasly.Web --launch-profile https`.
2. In one browser, log in to MVC as `marites.mendoza@klasly.test` (password `Klasly@123`) and open **My children**.
3. In a private window, log in as `admin@klasly.test`. Go to **Accounts › Parents** and click **Deactivate** on Marites Mendoza.
4. Back in the first browser, click any link. You are sent to the login page with "Your session ended, please log in again".
5. As admin, edit Marites and tick **Active** again so the demo data is back to normal.

### B. Run the real-phone end-to-end check (Task 12, the last open item)

1. Join the PC to the phone's hotspot. Find the PC's IP with `ipconfig` ("Wireless LAN adapter Wi-Fi" → IPv4).
2. Once only, in PowerShell **as Administrator**: `New-NetFirewallRule -DisplayName "Klasly API 5201" -Direction Inbound -Protocol TCP -LocalPort 5201 -Action Allow -Profile Private`
3. Start the API: `dotnet run --project src/Klasly.API --launch-profile lan`.
4. On the phone's browser, open `http://<PC IP>:5201/api/auth/me`. It should show a JSON error with "Session expired – please log in". That proves the phone can reach the API.
5. In `android/KlaslyApp/local.properties`, set `klasly.apiBaseUrl=http://<PC IP>:5201/api/`. Then, from `android/KlaslyApp`: `./gradlew installDebug`. Unplug the cable.
6. Start MVC and the Worker on the PC as usual. As Rosa, open a session on class 1, which shows the rotating QR.
7. On the phone, log in as `aira.mendoza@klasly.test` and scan. You should see "Checked in · Present". **Screenshot.**
8. On the PC, the live list shows Aira as Present (Scan). **Screenshot.**
9. Close the session. Within 5 minutes the Worker marks the students who didn't scan as Absent. **Screenshot of the session after refreshing.**
10. As Rosa, post a grade for Aira. The phone gets a "New grade posted" notification (only if Firebase is set up). **Screenshot.**
11. On the phone, log in as `marites.mendoza@klasly.test`. The child overview shows Aira's attendance and grade. **Screenshot.**
12. Put the screenshots in this folder and tick the last box in `SPEC.md` and `tasks/todo-m15.md`.

## Example

**An old token after deactivation** (what the phone or MVC gets):

```http
GET /api/parent/children
Authorization: Bearer eyJ…   (token from before the deactivation)

HTTP/1.1 401 Unauthorized
Content-Type: application/problem+json

{ "status": 401, "detail": "Session expired – please log in" }
```

**Serilog console (Worker):**

```
[13:55:04 INF] Tick 2026-11-11 05:55:00Z: closed 1, cancelled 1, finalized 4 sessions
[13:55:05 INF] Tick 2026-11-11 05:55:00Z: emails sent 0, retrying 0, failed 50
```

## Troubleshooting

| Problem | Fix |
|---|---|
| The API no longer prints `info: Microsoft.Hosting.Lifetime…` | That's expected. Serilog prints `[hh:mm:ss INF] Now listening on: …` instead. Levels are under `Serilog:MinimumLevel` in `appsettings.json` now, not `Logging`. |
| The phone can't open `http://<PC IP>:5201/...` | Set the Wi-Fi network to **Private** in Windows settings, check that the firewall rule exists, and use the phone's hotspot instead of campus Wi-Fi (campus Wi-Fi often blocks phone-to-PC traffic). |
| A user is logged out right after the admin edits them | Only when **Active** was unticked; that's the new rule. Tick it again and they can log in. |
| `./gradlew` says `JAVA_HOME is not set` | In PowerShell: `$env:JAVA_HOME = "D:\Android Studio\jbr"` |
