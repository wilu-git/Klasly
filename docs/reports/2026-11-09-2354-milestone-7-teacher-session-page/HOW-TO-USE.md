# How to use: running a class check-in from the website

## Prerequisites
- SQL Server LocalDB with the `Klasly` database (Milestone 2).
- The Web API running: `dotnet run --project src/Klasly.API --launch-profile https` (https://localhost:7256).
- The website running: `dotnet run --project src/Klasly.Web --launch-profile https` (https://localhost:7144).
- Students check in with the Android app (Milestone 8). Until then, a scan can be simulated with Swagger or curl (see the example below).

## Step by step
1. Log in at https://localhost:7144 as a teacher (for example `rosa.villanueva@klasly.test`). You land on **My classes**.
2. Click **Sessions** next to a class. You see the class's sessions, newest first, with Manila times and a status badge.
3. In **Start a session**, check **Ends at** (it defaults to 90 minutes from now) and **Late after (minutes)** (default 15). Click **Start session**.
4. The session page opens with a large QR code on the left and the class list on the right. Project or show the QR to the class. It changes every 30 seconds, and a code scanned up to 30 seconds late still works.
5. As students scan, their row changes from **Not yet** to **Present** (green) or **Late** (amber) within 5 seconds, with the scan time. The line above the table counts them: "12 of 30 checked in · 10 Present · 2 Late".
6. When class is over, click **End session** (top right) and confirm. The page shows "This session has ended." with the final list. If you forget, the page switches to "ended" by itself when the end time passes.
7. A session created earlier (still **Scheduled**) has an **Open session** button on its page.

## Example (simulating a student's scan)
```bash
# 1. Log in as the student → copy "token"
curl -k -H "Content-Type: application/json" -d '{"email":"aira.mendoza@klasly.test","password":"<test password>"}' https://localhost:7256/api/auth/login
# 2. The current QR text, as the teacher (same value the page's QR contains)
curl -k -H "Authorization: Bearer <teacher token>" https://localhost:7256/api/sessions/<id>/qr
# 3. Scan with the student's registered device id
curl -k -H "Authorization: Bearer <student token>" -H "Content-Type: application/json" \
  -d '{"qrToken":"<token from step 2>","deviceId":"<registered device>"}' https://localhost:7256/api/attendance/scan
```
Response: `{"status":"Present","scannedAtUtc":"…","subjectCode":"ELECT3","section":"BSCpE 3-A"}`. Within 5 seconds, the teacher's page shows that student as Present.

## Troubleshooting
- **"Reconnecting… The QR and list below may be out of date."**: the website can't reach the API. Check that the API is running. The page recovers by itself, and the first calls after an API restart can take about 10 seconds.
- **The form says "The end time is already in the past."**: the Ends at time must be later than now (Manila time). Fix it and click Start session again.
- **A student gets "Unregistered device" when scanning**: their phone isn't the one registered to their account. An admin can reset it (Swagger for now).
