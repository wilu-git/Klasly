# Milestone 6 — Web API sessions, rotating QR token, scan check-in

## Summary

The team asked for the server half of QR attendance (SPEC.md, Milestone 6), built with `/build auto` from `tasks/todo.md` in 7 code tasks with one commit each. The API now does the following:
- lets a teacher create, open and close sessions for their own classes;
- hands out an HMAC-signed QR token that rotates every `QrStepSeconds` (30 s);
- registers a student's phone right after login;
- records a scan as Present or Late after the guide's 7 checks;
- shows the teacher a live roster.

There is no database change and no new package.

## Projects touched

WEB API (`Klasly.API`), EF CORE (`Klasly.Infrastructure`), domain (`Klasly.Core`). Not touched: WEB MVC, CONSOLE WORKER, ANDROID KOTLIN, MSSQL schema.

## Files changed

| Path | Change | Why |
|---|---|---|
| `src/Klasly.API/Program.cs` | modified | `Qr:SigningKey` startup check, JWT `OnChallenge` problem-details, new service registrations |
| `src/Klasly.API/Middleware/ApiExceptionHandler.cs` | modified | `ForbiddenException` → 403, `UnauthorizedAccessException` → 401 |
| `src/Klasly.API/Services/ForbiddenException.cs` | added | "logged in but not yours" → 403 |
| `src/Klasly.API/Services/QrTokenService.cs` + `Interfaces/IQrTokenService.cs` | added | create / parse / verify the QR token (HMAC-SHA256, constant-time compare) |
| `src/Klasly.API/Services/SessionService.cs` + interface | added | ownership, create, open, close, QR, roster |
| `src/Klasly.API/Services/AttendanceService.cs` + interface | added | the 7 scan checks + insert |
| `src/Klasly.API/Services/AccountService.cs` + interface | modified | register / reset device |
| `src/Klasly.API/Services/ClassService.cs` + interface | modified | `ListForTeacherAsync` for "my classes" |
| `src/Klasly.API/Controllers/SessionsController.cs` | added | teacher routes: my classes, sessions, open, close, qr, attendance |
| `src/Klasly.API/Controllers/AttendanceController.cs` | added | `POST api/attendance/scan` |
| `src/Klasly.API/Controllers/StudentsController.cs` | added | `POST api/students/me/device` |
| `src/Klasly.API/Controllers/AccountsController.cs` | modified | `DELETE api/accounts/{id}/device` |
| `src/Klasly.Core/DTOs/` `CreateSessionRequest`, `SessionResponse`, `QrTokenResponse`, `SessionAttendanceRow`, `ScanRequest`, `ScanResponse`, `RegisterDeviceRequest`, `DeviceResponse` | added | request/response shapes (never the nonce or the key) |
| `src/Klasly.Core/Interfaces/` `ISessionRepository`, `IAttendanceRepository`, `ISystemSettingRepository` | added | data-access contracts |
| `src/Klasly.Core/Interfaces/IClassRepository.cs` | modified | `ListByTeacherAsync` |
| `src/Klasly.Infrastructure/Repositories/` `SessionRepository`, `AttendanceRepository`, `SystemSettingRepository` | added | EF Core implementations |
| `src/Klasly.Infrastructure/Repositories/ClassRepository.cs` | modified | `ListByTeacherAsync` (active term first) |
| `src/Klasly.Infrastructure/DependencyInjection.cs` | modified | registers the 3 new repositories |
| `SPEC.md`, `tasks/plan.md`, `tasks/todo.md` | modified | approved spec + decisions, Milestone 6 plan, ticked tasks |

## Key code

**Endpoints**

| Route | Who | Notes |
|---|---|---|
| `GET api/teacher/classes` | Teacher | own classes, active term first |
| `GET/POST api/classes/{classId}/sessions` | Teacher (owner) | create: end > start, end > now, late 0–120 (default 15) |
| `GET api/sessions/{id}` | Teacher (owner) | |
| `POST api/sessions/{id}/open` / `close` | Teacher (owner) | idempotent; 409 "Session is closed" / "Only an open session can be closed." |
| `GET api/sessions/{id}/qr` | Teacher (owner) | `{ token, expiresAtUtc, stepSeconds }` while effectively open |
| `GET api/sessions/{id}/attendance` | Teacher (owner) | one row per Active student, `status: null` = not scanned |
| `POST api/students/me/device` | Student | first phone saved; same = no-op; other = 409 |
| `DELETE api/accounts/{id}/device` | Admin | 204, also when none registered |
| `POST api/attendance/scan` | any JWT; non-Student → 401 | the 7 checks |

**Token** (`QrTokenService`):

```csharp
public string Create(int sessionId, long step, string nonce) => $"{sessionId}.{step}.{Sign(sessionId, step, nonce)}";

private string Sign(int sessionId, long step, string nonce) =>
    Base64Url.EncodeToString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{sessionId}|{step}|{nonce}")));
```

A token is about 55 characters, for example `1.59807712.WFcYwVdw…vda4`. The verify side compares with `CryptographicOperations.FixedTimeEquals` and accepts `step == current || step == current - 1`.

**Scan order** (`AttendanceService.ScanAsync`), as in guide §27.8:
1. role Student and an active account → else 401 "Session expired – please log in";
2. parse, session exists, signature valid → else 400 "Invalid QR code" (never a 404);
3. current or previous step → else 400 "QR expired – rescan";
4. `SessionService.IsOpenNow` (Open and now < EndTime) → else 409 "Session is closed";
5. Active enrollment → else 403 "Not enrolled in this class";
6. `deviceId == RegisteredDeviceId` (null fails) → else 403 "Unregistered device";
7. no record yet → else 409 "Already checked in". `AttendanceRepository.TryAddAsync` returns false on a unique-index violation (SQL 2601/2627), so a lost race also gets 409.

`ScanRequest` has no validation attributes on purpose. A missing value must fail the checks in their order, not as an earlier model-validation 400.

**401 body**: JWT `OnChallenge` writes `{ status: 401, detail: "Session expired – please log in" }` on every endpoint. The website only looks at the status code (`ApiExceptionFilter`), so nothing changes there.

## Decisions

- **Q1:** the teacher creates sessions.
- **Q2 (changed by the team):** the phone is registered before the first scan through `POST api/students/me/device`, and the scan never registers one.
- **Q3:** the Milestone 7 read endpoints are built now.
- **Q4:** teacher Close endpoint, and an Open session past its `EndTime` counts as closed.
- **Small changes from the plan:**
  - All teacher routes sit in one `SessionsController`; there is no separate `TeacherController`.
  - `ISystemSettingRepository.GetValueAsync` returns the raw string, and the service parses it (the Worker will reuse it for thresholds).
  - `DeviceResponse` was added for the device endpoint's `200 { deviceId }`.

## Database changes

None. The existing `ClassSessions.QrNonce`, `AttendanceRecords` unique `(SessionId, StudentId)`, `Users.RegisteredDeviceId` and `SystemSettings.QrStepSeconds` are used as-is.

## How it was verified

- `dotnet build Klasly.slnx` after every task → 0 warnings, 0 errors.
- `dotnet list Klasly.slnx package` → every Microsoft package is 10.0.14; no package added.
- `git grep` for the local `Qr:SigningKey` value → not found. The key lives only in user-secrets.
- Manual smoke test with curl against LocalDB. All 15 SPEC steps passed:
  - 401 body;
  - ownership 403s;
  - create validation;
  - open/close/QR, with the token rotating at the step boundary;
  - device register / 409 / reset;
  - Present, Late, already checked in, unregistered device (including a student with no phone);
  - not enrolled (including a Dropped student);
  - teacher/parent/no token → 401;
  - forged signature, edited id, `hello`, unknown id → 400;
  - grace step works, two-step-old token → expired;
  - closed session → 409;
  - two parallel scans → one 200 + one 409, and the database holds 1 row;
  - API refuses to start without the key;
  - admin reset → new phone scans.
- Decision Q4 was checked by scanning a still-valid (grace step) token after the session's end time → 409 "Session is closed".
- No automated tests: the spec has no test project. Role and boundary tests are Milestone 14.

## Known limits / TODO

- `shortcut:` in `AccountService.RegisterDeviceAsync`: two different phones registering one account at the same instant → last write wins.
- Two student accounts can register the same phone (`RegisteredDeviceId` isn't unique).
- The Android ID depends on the app's signing key: debug and release builds give different IDs, so after switching builds the admin resets each test student once.
- Ended sessions stay `Open` in the database until the Worker closes them (Milestone 9). They behave as closed for QR and scans.
- No website button for the device reset yet (Swagger only).
- The smoke test left test sessions and attendance rows in the local database, plus one Dropped enrollment (student 2 in class 3).
