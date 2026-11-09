# Spec: Milestone 6 — Web API sessions, rotating QR token, scan check-in

> Status: **Done** (2026-11-09; reports in `docs/reports/2026-11-09-2144-milestone-6-api-sessions/`). Q1, Q3 and Q4 approved as recommended; Q2 changed to "register the phone before the first scan through a separate endpoint" (see Decisions at the bottom). Tasks: [`tasks/todo.md`](tasks/todo.md). Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.8 "QR attendance check-in") → `CLAUDE.md` → this spec.
> Milestone 5's spec (Web admin pages) is in git history (commit `f859150`) and in `docs/reports/2026-11-09-2101-milestone-5-web-admin/`. The QR flow is drawn in `docs/4-2 Core Feature Flowchart - QR Check-in.png`.

## Assumptions (correct these before we build)

1. **Web API only.** No MVC pages (Milestone 7) and no Android screens (Milestone 8). Everything is testable from Swagger UI or curl.
2. **No database change.** `ClassSessions`, `AttendanceRecords` (unique `SessionId + StudentId`), `Users.RegisteredDeviceId` and the `QrStepSeconds = 30` setting already exist from Milestone 2. If a change turns out to be needed, stop and ask.
3. **Token format follows guide §27.8:** `{sessionId}.{timeStep}.{signature}`, where
   - `timeStep = floor(UnixSeconds(UtcNow) / QrStepSeconds)`, with `QrStepSeconds` read from `SystemSettings`;
   - `signature = Base64Url(HMACSHA256(Qr:SigningKey, "{sessionId}|{timeStep}|{QrNonce}"))`, the full 32 bytes;
   - a token is about 60 characters, which fits a small QR code easily.

   The API accepts the current step and the previous step. A token shown at the start of step *s* therefore works for 30 to 60 seconds. Signatures are compared with `CryptographicOperations.FixedTimeEquals`.
4. **`Qr:SigningKey` lives in user-secrets only** (CLAUDE.md §3.3). Like `Jwt:Key`, the API refuses to start if the key is missing or shorter than 32 characters, and the error names the `dotnet user-secrets set` command.
5. **Each session gets its own random `QrNonce`** (32 random bytes as hex = 64 characters, the column maximum), set when the session is created. Without the nonce, a token for session 5 at step *s* could be worked out by anyone who once saw the key. With it, each session's tokens are independent. The nonce is never returned by any endpoint.
6. **Only the class's own teacher manages its sessions.** "Owner" means `Classes.TeacherId` matches the `Teachers` row of the JWT's `sub`. Another teacher gets `403`, and admins, students and parents get `403` from the `TeacherOnly` policy. A session id that doesn't exist gets `404`. The admin does not run sessions.
7. **The scan endpoint answers `401` for a non-Student role** (CLAUDE.md §3.3 and guide check 1), not `403`. It uses plain `[Authorize]`, and the service checks the role.
   - The JWT handler's `OnChallenge` event is changed to write problem-details with `"Session expired – please log in"`. A missing or expired token then carries the same message as the role failure, and the app can show the API's message (CLAUDE.md §3.6).
   - This changes the empty `401` body on **every** endpoint to a problem-details body with that message. The website already shows its own "Your session ended" text for `401`, so nothing visible changes there.
8. **The scan checks run in exactly the guide's order** and stop at the first failure (see "Scan endpoint" below). A malformed token, or a token naming a session that doesn't exist, fails check 2 (`400 "Invalid QR code"`). It is never a `404`, so nobody can probe which session ids exist.
9. **Present or Late uses the server's clock:** `ScannedAt = UtcNow`. If `ScannedAt ≤ StartTime + LateAfterMinutes` the status is **Present**, otherwise **Late**. Scanning before `StartTime` (the session was opened early) counts as Present.
10. **Two scans racing each other:** the unique index rejects the second insert, and the service turns that database error into `409 "Already checked in"`. That message is specific; the generic "That value is already used." would not do here.
11. **Session times travel as `DateTimeOffset`** in requests (for example `2026-11-10T09:00:00+08:00`) and are stored as UTC. Responses return UTC (`...Z`). Only the MVC page and the app convert to Asia/Manila.
12. **New exception types for the existing `ApiExceptionHandler`:**
    - `ForbiddenException` → `403`;
    - `UnauthorizedAccessException` (built into .NET) → `401`.

    Controllers stay one line per action, the same as Milestone 4.
13. **No test project** (same as Milestones 3–5). Manual smoke test with Swagger and curl. Automated role and boundary tests come in Milestone 14.
14. **Out of scope:** cancelling or editing a session, auto-closing ended sessions and marking Absent (Worker, Milestone 9), teacher attendance corrections (Milestone 9), the MVC QR page (Milestone 7), the Android scanner (Milestone 8), GPS or offline scans (Optional features).

## Objective

Build the server half of Klasly's core feature: a teacher opens a class session, the API hands out a signed QR token that changes every 30 seconds, and a student's phone sends that token back to check in as Present or Late. Every bad scan (forged, expired, closed, not enrolled, wrong phone, twice) is refused with a clear message.

After this milestone, Milestone 7 (teacher QR page) and Milestone 8 (Android scanner) can be built against a finished, tested API without touching it.

Functional requirements covered: guide §27.9 **#3** (open a session with a signed QR that refreshes every 30 s, API side), **#4** (record Present/Late for a valid scan; reject invalid, expired, duplicate or non-enrolled scans with a clear message) and **#1** (ownership checks on the server).

### User stories
- As a **teacher**, I list my own classes and their sessions.
- As a **teacher**, I create a session for my class (start, end, minutes until Late) and open it when class begins.
- As a **teacher**, while the session is open, I get a fresh QR token every 30 seconds to show on screen.
- As a **teacher**, I see who has checked in so far (Present or Late, with the time) and who hasn't.
- As a **teacher**, I close the session when class ends.
- As a **student**, my phone is registered right after I log in on the app, before my first scan.
- As a **student**, I scan the QR on my registered phone and get "Present" or "Late" back.
- As an **admin**, I reset a student's phone when they get a new one.
- As a **student**, a bad scan tells me what went wrong: "Invalid QR code", "QR expired – rescan", "Session is closed", "Not enrolled in this class", "Unregistered device", "Already checked in".
- As **another teacher**, I cannot see, open or read the QR of a class that isn't mine.

## Endpoints

All routes are under `/api/`, return JSON and use problem-details for errors (`{ status, detail }`). The ones marked ⓘ depend on the open questions below.

### Teacher (policy `TeacherOnly` + ownership)

| Method & route | Body | Success | Errors |
|---|---|---|---|
| ⓘ `GET /api/teacher/classes` | — | `200` my classes (`ClassResponse` from Milestone 4), active term first, then by subject code | — |
| `GET /api/classes/{classId}/sessions` | — | `200` sessions of the class, newest start first | `403` not my class · `404` class |
| `POST /api/classes/{classId}/sessions` | `{ startTime, endTime, lateAfterMinutes? }` | `201` session, status `Scheduled` | `400` end ≤ start, end in the past, late minutes outside 0–120 · `403` · `404` |
| `GET /api/sessions/{id}` | — | `200` session | `403` · `404` |
| `POST /api/sessions/{id}/open` | — | `200` session, status `Open` (already Open → `200`, no change) | `400` "This session has already ended." (now ≥ EndTime) · `409` "Session is closed" (Closed / Finalized / Cancelled) · `403` · `404` |
| ⓘ `POST /api/sessions/{id}/close` | — | `200` session, status `Closed`, `closedAt` = now (already Closed → `200`, no change) | `409` "Only an open session can be closed." · `403` · `404` |
| `GET /api/sessions/{id}/qr` | — | `200` `{ token, expiresAtUtc, stepSeconds }` | `409` "Session is closed" (not Open) · `403` · `404` |
| ⓘ `GET /api/sessions/{id}/attendance` | — | `200` one row per **Active** enrolled student, by name: `{ studentId, studentNumber, fullName, status, scannedAtUtc, source }`. `status` is `null` = hasn't scanned yet | `403` · `404` |

`SessionResponse`: `{ sessionId, classId, subjectCode, section, startTimeUtc, endTimeUtc, lateAfterMinutes, status, closedAtUtc }`. `QrNonce` is never included.

`expiresAtUtc` on the QR answer is the end of the current step, when the page should ask for the next token. The token stays accepted for one more step after that (the grace step).

### Student

| Method & route | Body | Success | Errors |
|---|---|---|---|
| `POST /api/students/me/device` (policy `StudentOnly`) | `{ deviceId }` (required, ≤ 200 chars, the phone's Android ID) | `200` `{ deviceId }`: saved when the account had no phone; same phone again → `200`, no change | `409` "Another device is already registered. Ask the admin to reset it." · `403` non-Student |
| `POST /api/attendance/scan` | `{ qrToken, deviceId }` (`deviceId` required, ≤ 200 chars) | `200` `{ status: "Present" \| "Late", scannedAtUtc, subjectCode, section }` | see the checks below |

The Android app (Milestone 8) calls the device endpoint after **every** student login. Its answer tells the app whether this phone may scan, so there is no separate "am I registered?" endpoint.

#### Scan endpoint — checks in this exact order (guide §27.8)

| # | Check | On failure |
|---|---|---|
| 1 | Valid JWT, role = Student, account active with a Students row | `401` "Session expired – please log in" |
| 2 | Token has 3 parts, session exists, HMAC signature matches | `400` "Invalid QR code" |
| 3 | `timeStep` is the current or the previous step | `400` "QR expired – rescan" |
| 4 | Session status = Open (ⓘ and now < EndTime) | `409` "Session is closed" |
| 5 | Student has an **Active** enrollment in the session's class | `403` "Not enrolled in this class" |
| 6 | `deviceId` equals the account's `RegisteredDeviceId` (no phone registered → fails too; the scan never registers one) | `403` "Unregistered device" |
| 7 | No AttendanceRecord yet for (session, student) | `409` "Already checked in" (also for a lost insert race) |

On success, insert an `AttendanceRecord` with `Status` Present or Late, `Source = Scan`, `ScannedAt = MarkedAt = UtcNow`, `MarkedBy = null`.

### Admin

| Method & route | Success | Errors |
|---|---|---|
| `DELETE /api/accounts/{id}/device` | `204`, `RegisteredDeviceId` cleared (also `204` when none was registered), so the student's next login registers their new phone. `AccountResponse` keeps hiding the device id | `404` account |

## Commands

```bash
dotnet user-secrets set "Qr:SigningKey" "<at least 32 random characters>" --project src/Klasly.API
dotnet build Klasly.slnx
dotnet list Klasly.slnx package
dotnet run --project src/Klasly.API --launch-profile https   # https://localhost:7256/swagger
```
No migration. No new package: `HMACSHA256`, `RandomNumberGenerator`, `CryptographicOperations` and `Base64Url` all ship with .NET 10.

## Project structure (files this milestone touches)

```
src/Klasly.Core/
  DTOs/          (new) CreateSessionRequest, SessionResponse, QrTokenResponse,
                 SessionAttendanceRow, ScanRequest, ScanResponse, RegisterDeviceRequest
  Interfaces/    (new) ISessionRepository, IAttendanceRepository, ISystemSettingRepository
src/Klasly.Infrastructure/
  Repositories/  (new) SessionRepository, AttendanceRepository, SystemSettingRepository
  DependencyInjection.cs        (modified) register the 3 repositories
src/Klasly.API/
  Program.cs                    (modified) Qr:SigningKey startup check, JWT OnChallenge message, new services
  Middleware/ApiExceptionHandler.cs   (modified) ForbiddenException → 403, UnauthorizedAccessException → 401
  Services/ForbiddenException.cs      (new)
  Services/QrTokenService.cs (+ interface)   (new) create / verify token — pure HMAC code, no database
  Services/SessionService.cs (+ interface)   (new) ownership, create, open, close, QR, roster
  Services/AttendanceService.cs (+ interface) (new) the 7 scan checks + insert
  Controllers/TeacherController.cs    (new) GET api/teacher/classes
  Controllers/SessionsController.cs   (new) class sessions + session routes
  Controllers/AttendanceController.cs (new) POST api/attendance/scan
  Controllers/StudentsController.cs   (new) POST api/students/me/device
  Controllers/AccountsController.cs   (modified) DELETE {id}/device
  Services/AccountService.cs (+ interface)    (modified) register / reset device
```

## Code style

Same as Milestones 3–4: file-scoped namespaces, primary constructors, `async` + `CancellationToken` all the way down, services throw and the exception handler maps the exception to a status code. The scan service reads top to bottom like the guide's table:

```csharp
public async Task<ScanResponse> ScanAsync(int userId, string role, ScanRequest request, CancellationToken ct)
{
    var student = role == "Student" ? await users.GetActiveStudentAsync(userId, ct) : null;
    if (student is null) throw new UnauthorizedAccessException("Session expired – please log in");   // 1

    if (!qr.TryParse(request.QrToken, out var sessionId, out var step)) throw new ValidationException("Invalid QR code");
    var session = await sessions.GetByIdAsync(sessionId, ct);
    if (session is null || !qr.HasValidSignature(sessionId, step, session.QrNonce, request.QrToken))
        throw new ValidationException("Invalid QR code");                                                 // 2

    var current = await qr.CurrentStepAsync(ct);
    if (step != current && step != current - 1) throw new ValidationException("QR expired – rescan");    // 3
    // 4–7 follow the same pattern, then insert and return Present / Late.
}
```

## Testing strategy

- **Build:** `dotnet build Klasly.slnx` → 0 errors, 0 new warnings. `dotnet list package` → no Microsoft package below 10.x. `git grep` finds no signing key in the repo.
- **Manual smoke test** in Swagger or curl, against LocalDB with the Milestone 2 seed plus the Milestone 5 test rows. The seed teachers, students and their classes are used as-is; sessions are created during the test.
  1. Teacher A: `GET /api/teacher/classes` → only A's classes. Teacher B on A's class sessions → `403`. Admin token → `403`. No token → `401` with the "Session expired" message.
  2. Create a session for A's class (start = now, end = now + 90 min) → `201 Scheduled`. End before start → `400`. End in the past → `400`. `lateAfterMinutes: 121` → `400`. Unknown class → `404`.
  3. `GET qr` while Scheduled → `409`. Open → `200 Open`. Open again → `200`. `GET qr` → token. Two calls in the same 30 s → the same token. After the step changes → a different token.
  4. Enrolled student 1 registers `phone-1` → `200`; again → `200`. Scans with `phone-1` → `200 Present`. The attendance roster shows them Present with the time and everyone else with `status: null`.
  5. Student 1 scans again → `409 Already checked in`.
  6. Student 1 registers `phone-2` → `409` "Another device is already registered…". Student 1 scans another open session with `phone-2` → `403 Unregistered device`. Enrolled student 2, who never registered a phone, scans → `403 Unregistered device`. A teacher token on the device endpoint → `403`.
  7. A student not enrolled in the class (and a student Dropped from it) → `403 Not enrolled in this class`.
  8. Teacher or parent token on scan → `401 "Session expired – please log in"`. No token → the same.
  9. Change one character of the signature → `400 Invalid QR code`. Change the session id in the token → `400`. Send the text `hello` → `400`. A token for a session id that doesn't exist → `400` (not `404`).
  10. Keep a token, wait 65 s, scan → `400 QR expired – rescan`. A token 31–59 s old still works (grace step).
  11. A session created with start = now − 30 min and late = 15 → scan → `200 Late`.
  12. Get a token, close the session, scan with that token → `409 Session is closed`. `GET qr` after closing → `409`. Open after closing → `409`.
  13. Two scans for the same student and session sent at the same moment (two `curl` processes) → one `200`, one `409 Already checked in`, one row in the database.
  14. Start the API without `Qr:SigningKey` → it refuses to start with the user-secrets command in the message.
  15. Admin clears student 1's device → `204` (again → `204`) → student 1 registers `phone-2` → `200` → scans a new session with `phone-2` → `200`.

## Boundaries

- **Always:** Controller → Service → Repository (guide §2); business rules only in `Klasly.API` services; DTOs in and out (never entities, never `QrNonce`); UTC in the database and in responses; the guide's exact error messages and check order; constant-time signature comparison; `async` + `CancellationToken`; `dotnet build` after each change; the three reports when built.
- **Ask first:** any schema change or migration; any new package; any endpoint not in the tables above; changing a Milestone 3–4 endpoint other than the 401 body described in assumption 7.
- **Never:** commit `Qr:SigningKey` or put it in `appsettings*.json`; return the nonce or the key; accept a token older than the previous step; trust the client's clock or a `scannedAt` sent by the client; turn on CORS; return `404` for an unknown session id in a scan; let a database error leak SQL into a response.

## Success criteria

- [x] All smoke-test steps behave as written.
- [x] Every scan failure returns the guide's status code and message, in the guide's order.
- [x] A forged, edited or expired token is never accepted; tokens rotate every `QrStepSeconds`.
- [x] Only the class's own teacher can create, open, close, read the QR of, or read the roster of a session.
- [x] A student has at most one AttendanceRecord per session, even with parallel scans.
- [x] No migration, no new package, no key in the repo; every Microsoft package is 10.x.
- [x] Reports written; `docs/reports/INDEX.md` updated.

## Decisions (2026-11-09)

- **Q1:** recommended — the teacher creates sessions.
- **Q2: changed by the team.** The phone is registered **before** the first attendance, through `POST /api/students/me/device`, which the app calls after every student login. The scan only compares (check 6). The admin resets with `DELETE /api/accounts/{id}/device`.
  - *Known limits:* two student accounts can still register the same phone (the column isn't unique). The Android ID depends on the app's signing key, so a debug build and a release build give **different** IDs: after switching builds, the admin resets each test student once.
- **Q3:** recommended — build the Milestone 7 read endpoints now.
- **Q4:** recommended — teacher Close endpoint, and an Open session past `EndTime` counts as closed.

## Open questions as asked (kept for the record)

1. **Where do sessions come from?** The guide fixes `POST /api/sessions/{id}/open`, so a session must exist before it is opened, but nothing says who creates it.
   **Recommended:** the teacher creates it, `POST /api/classes/{classId}/sessions` with start, end and late minutes, and opens it when class starts. The Milestone 7 page can prefill "now → now + 90 min".
   *Alternatives:* the admin creates sessions; or the Worker generates them from the class `Schedule` text (it is free text, so this is hard to parse).
2. **How does a phone become the "registered device"?** `Users.RegisteredDeviceId` exists, but nothing sets it.
   **Recommended:** the first successful scan registers the device (trust on first use), in the same save as the attendance row. The admin can clear it when a student changes phones, with one `DELETE /api/accounts/{id}/device` endpoint; the website button comes later.
   *Alternatives:* send the device id with the student's login (changes the Milestone 3 login endpoint); or the admin types the device id.
   *Known limit either way:* the column isn't unique, so two student accounts could register the same phone.
3. **Should this milestone also build the read endpoints Milestone 7 needs:** my classes, the class's session list, and the live attendance roster?
   **Recommended: yes**, so Milestone 7 is web-only, the same split as Milestones 4 → 5.
   *Alternative:* add them in Milestone 7.
4. **Closing a session before the Worker exists (Milestone 9):**
   **Recommended:** (a) add `POST /api/sessions/{id}/close` for the teacher's "End session" button (it is also the only way to test check 4 now), and (b) treat an Open session whose `EndTime` has passed as closed for scans and QR, so a forgotten session can't take scans hours later while the Worker isn't running yet.
   *Alternative:* only the Worker closes sessions, and check 4 looks at `Status` alone.
