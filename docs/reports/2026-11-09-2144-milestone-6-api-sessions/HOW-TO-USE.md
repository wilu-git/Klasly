# How to use: QR attendance (Web API)

This milestone is the server side only. The teacher's QR page (Milestone 7) and the phone scanner (Milestone 8) come next. Until then you try everything in **Swagger UI** or with **curl**.

## Prerequisites

1. SQL Server LocalDB with the `Klasly` database (Milestone 2 migration applied).
2. The two API secrets, set once per computer:
   ```bash
   dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>" --project src/Klasly.API
   dotnet user-secrets set "Qr:SigningKey" "<at least 32 random characters>" --project src/Klasly.API
   ```
3. Run the API: `dotnet run --project src/Klasly.API --launch-profile https`, then open `https://localhost:7256/swagger`.
4. All seed accounts use the password `Klasly@123`.
   - Teacher of ELECT3 (class 1): `rosa.villanueva@klasly.test`.
   - Students in that class: `aira.mendoza@klasly.test`, `bryan.lacsamana@klasly.test`, and others.

## Step by step

1. **Log in as the teacher.** In Swagger, `POST /api/auth/login`, then copy `token` into the **Authorize** button.
2. **Find your class.** `GET /api/teacher/classes` lists only your classes.
3. **Create a session.** `POST /api/classes/1/sessions` with
   `{ "startTime": "2026-11-10T09:00:00+08:00", "endTime": "2026-11-10T10:30:00+08:00", "lateAfterMinutes": 15 }`.
   Times can carry the +08:00 offset; the answer shows them in UTC (ending in `Z`).
4. **Open it when class starts.** `POST /api/sessions/{id}/open`.
5. **Get the QR token.** `GET /api/sessions/{id}/qr`. Ask again at `expiresAtUtc` (every 30 s). In Milestone 7 the web page does this and draws the QR.
6. **Register the student's phone (once).** Log in as a student, Authorize with their token, then `POST /api/students/me/device` with `{ "deviceId": "phone-1" }`. The Android app will do this automatically after every login.
7. **Scan.** As the student, `POST /api/attendance/scan` with `{ "qrToken": "<token from step 5>", "deviceId": "phone-1" }`.
8. **Watch the roster.** As the teacher, `GET /api/sessions/{id}/attendance`.
9. **End the session.** `POST /api/sessions/{id}/close`.
10. **Student changed phones?** As the admin, `DELETE /api/accounts/{userId}/device`. The student's next login registers the new phone.

## Example

Scan request and answer:

```http
POST /api/attendance/scan
Authorization: Bearer <student JWT>
{ "qrToken": "4.59807720.k3m…Q8", "deviceId": "phone-1" }

200 OK
{ "status": "Present", "scannedAtUtc": "2026-11-09T13:40:41Z", "subjectCode": "ELECT3", "section": "BSCpE 3-A" }
```

| What went wrong | Answer |
|---|---|
| not a student / no login | `401` "Session expired – please log in" |
| edited or fake QR | `400` "Invalid QR code" |
| QR older than about 60 s | `400` "QR expired – rescan" |
| session not open (or past its end) | `409` "Session is closed" |
| not in this class | `403` "Not enrolled in this class" |
| a different phone, or none registered | `403` "Unregistered device" |
| scanned already | `409` "Already checked in" |

## Troubleshooting

- **The API stops at startup with "Qr:SigningKey is missing…".** Run the `dotnet user-secrets set "Qr:SigningKey" …` command from the prerequisites.
- **"Unregistered device" on a phone that worked before.** The phone's ID changed. A debug and a release build of the app give different IDs, and so does a factory reset. Ask the admin to reset the device, then log in again.
- **"QR expired – rescan" right away.** The token is only good for its 30-second step plus one more. Fetch a fresh one from `GET /api/sessions/{id}/qr`; the server's clock decides, not the phone's.
