# Milestone 6 — Task list

> Plan: [`plan.md`](./plan.md) · Spec: [`SPEC.md`](../SPEC.md)
> Build command for every task: `dotnet build Klasly.slnx`. No test project, so each task is checked with curl or Swagger against the API on `https://localhost:7256` (`dotnet run --project src/Klasly.API --launch-profile https`). Seed logins use the password `Klasly@123`.
> "Step N" means smoke-test step N in SPEC.md → Testing strategy.

---

## Task 1: Error plumbing and secrets check

**Description:** The pieces every later task needs: two more exception mappings, a 401 body the app can show, and a startup check for the QR signing key.

**Acceptance criteria:**
- [x] `ForbiddenException` (new) → `403`, and `UnauthorizedAccessException` → `401` in `ApiExceptionHandler`.
- [x] JWT `OnChallenge` writes problem-details `{ status: 401, detail: "Session expired – please log in" }`.
- [x] The API refuses to start when `Qr:SigningKey` is missing or shorter than 32 characters, and the message names the `dotnet user-secrets set` command.

**Verification:**
- [x] Build succeeds
- [x] No token on `GET /api/classes` → `401` with the message
- [x] Step 14 (start without the key → refused)

**Dependencies:** None

**Files likely touched:** `src/Klasly.API/Program.cs`, `Middleware/ApiExceptionHandler.cs`, `Services/ForbiddenException.cs` (new)

**Estimated scope:** S

---

## Task 2: QR token service

**Description:** Create and verify the rotating QR token. Pure HMAC code plus one settings read.

**Acceptance criteria:**
- [x] `ISystemSettingRepository.GetValueAsync(key)` + EF implementation, registered in `AddInfrastructure`.
- [x] `IQrTokenService`: `StepSecondsAsync`, `CurrentStep(stepSeconds)`, `Create(sessionId, step, nonce)`, `TryParse(token, out sessionId, out step)`, `HasValidSignature(token, nonce)`.
- [x] HMACSHA256 over `"{sessionId}|{step}|{nonce}"`, Base64Url, `FixedTimeEquals`; the key comes from `Qr:SigningKey`.

**Verification:**
- [x] Build succeeds
- [x] Behaviour checked through Tasks 4 and 6 (steps 3, 9, 10)

**Dependencies:** Task 1

**Files likely touched:** `src/Klasly.Core/Interfaces/ISystemSettingRepository.cs` (new), `src/Klasly.Infrastructure/Repositories/SystemSettingRepository.cs` (new), `DependencyInjection.cs`, `src/Klasly.API/Services/QrTokenService.cs` + `Interfaces/IQrTokenService.cs` (new), `Program.cs`

**Estimated scope:** S–M

---

## Checkpoint A — after Tasks 1–2
- [x] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [x] 401 body and missing-key startup error behave as specified

---

## Task 3: Create and read sessions, list my classes

**Description:** A teacher lists their own classes and a class's sessions, and creates a session. Only the class's own teacher may do so.

**Acceptance criteria:**
- [x] `ISessionRepository` / `SessionRepository` (`GetByIdAsync` tracked with Class + Subject, `ListByClassAsync`, `AddAsync`, `SaveChangesAsync`). `IClassRepository.ListByTeacherAsync`.
- [x] `CreateSessionRequest`, `SessionResponse` (never `QrNonce`). Create: end > start, end > now, late minutes 0–120 (default 15); nonce = 32 random bytes as hex; status `Scheduled`.
- [x] `GET api/teacher/classes`, `GET/POST api/classes/{classId}/sessions`, `GET api/sessions/{id}`, all in one `SessionsController` (no separate TeacherController); another teacher → `403`, unknown id → `404`, admin → `403`.

**Verification:**
- [x] Build succeeds
- [x] Steps 1–2

**Dependencies:** Task 1

**Files likely touched:** Core `Interfaces/ISessionRepository.cs`, `Interfaces/IClassRepository.cs`, `DTOs/SessionDtos.cs`; Infrastructure `Repositories/SessionRepository.cs`, `ClassRepository.cs`, `DependencyInjection.cs`; API `Services/SessionService.cs` + interface, `Controllers/SessionsController.cs`, `Controllers/TeacherController.cs`, `Program.cs`

**Estimated scope:** M

---

## Task 4: Open, close and the QR endpoint

**Description:** The teacher opens the session, gets a fresh token every step, and closes it.

**Acceptance criteria:**
- [x] `POST api/sessions/{id}/open`: Scheduled → Open; already Open → `200` no change; now ≥ End → `400` "This session has already ended."; Closed / Finalized / Cancelled → `409` "Session is closed".
- [x] `POST api/sessions/{id}/close`: Open → Closed with `ClosedAt`; already Closed → `200`; otherwise `409` "Only an open session can be closed."
- [x] `GET api/sessions/{id}/qr` → `{ token, expiresAtUtc, stepSeconds }`, only when effectively open (`Open` and now < End), else `409` "Session is closed".

**Verification:**
- [x] Build succeeds
- [x] Step 3 and the QR/open parts of step 12

**Dependencies:** Tasks 2, 3

**Files likely touched:** `SessionService.cs` + interface, `SessionsController.cs`, `SessionDtos.cs`

**Estimated scope:** S–M

---

## Checkpoint B — after Tasks 3–4
- [x] Steps 1–3 pass

---

## Task 5: Register and reset a student's device

**Description:** The phone is registered before the first scan (decision Q2), and the admin can reset it.

**Acceptance criteria:**
- [x] `POST api/students/me/device` (`StudentOnly`) `{ deviceId }`: no phone → saved, `200 { deviceId }`; same → `200`; different → `409` "Another device is already registered. Ask the admin to reset it."
- [x] `DELETE api/accounts/{id}/device` (`AdminOnly`) → `204`, also when none is registered; unknown id → `404`.
- [x] `AccountResponse` still never carries the device id.

**Verification:**
- [x] Build succeeds
- [x] Register `phone-1` → 200; again → 200; `phone-2` → 409; teacher token → 403; admin reset → 204 (twice); `phone-2` → 200; reset again for the later steps

**Dependencies:** Task 1

**Files likely touched:** Core `DTOs/RegisterDeviceRequest.cs` (new); API `Services/AccountService.cs` + interface, `Controllers/AccountsController.cs`, `Controllers/StudentsController.cs` (new)

**Estimated scope:** S

---

## Task 6: Scan endpoint with the 7 checks

**Description:** `POST /api/attendance/scan` runs the guide's checks in order and records Present or Late.

**Acceptance criteria:**
- [x] `IAttendanceRepository` / `AttendanceRepository` (`ExistsAsync`, `TryAddAsync` = false on a lost unique-index race), registered. `ListBySessionAsync` comes with Task 7.
- [x] `ScanRequest` / `ScanResponse`. Checks 1–7 in order with the guide's exact codes and messages; check 6 only compares (null → 403).
- [x] Insert `Status` Present (≤ Start + Late minutes) or Late, `Source = Scan`, `ScannedAt = MarkedAt = UtcNow`. A lost race → `409 "Already checked in"`.

**Verification:**
- [x] Build succeeds
- [x] Steps 4–13 (scan parts)

**Dependencies:** Tasks 2, 4, 5

**Files likely touched:** Core `Interfaces/IAttendanceRepository.cs`, `DTOs/ScanDtos.cs` (new); Infrastructure `Repositories/AttendanceRepository.cs` (new), `DependencyInjection.cs`; API `Services/AttendanceService.cs` + interface (new), `Controllers/AttendanceController.cs` (new), `Program.cs`

**Estimated scope:** M

---

## Task 7: Live attendance roster

**Description:** The teacher sees every Active student of the class with their scan result so far.

**Acceptance criteria:**
- [x] `GET api/sessions/{id}/attendance` → one row per Active enrollment, by name: `{ studentId, studentNumber, fullName, status, scannedAtUtc, source }`; `status: null` = not scanned yet.
- [x] Owner only (`403` / `404` as the other session routes).

**Verification:**
- [x] Build succeeds
- [x] Step 4 (roster part)

**Dependencies:** Task 6

**Files likely touched:** `SessionService.cs` + interface, `SessionsController.cs`, `SessionDtos.cs`

**Estimated scope:** S

---

## Checkpoint C — after Tasks 5–7
- [x] Steps 4–15 pass

---

## Task 8: Package check, full smoke test, reports, commit

**Description:** Prove everything works together, document it, commit.

**Acceptance criteria:**
- [ ] All 15 smoke steps pass in one run.
- [ ] `dotnet list Klasly.slnx package` → no Microsoft package below 10.x. `git grep` finds no signing key.
- [ ] `docs/reports/<yyyy-MM-dd-HHmm>-milestone-6-api-sessions/` with DEVELOPER-DOCS.md, HOW-TO-USE.md, FLOWCHARTS.md (no erDiagram: no DB change). One line in `INDEX.md`.
- [ ] SPEC.md success criteria ticked, status "Done"; tasks ticked here and in `plan.md`.

**Verification:**
- [ ] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [ ] Every SPEC success criterion ticked

**Dependencies:** Tasks 1–7

**Files likely touched:** `docs/reports/…` (new), `docs/reports/INDEX.md`, `SPEC.md`, `tasks/plan.md`, `tasks/todo.md`

**Estimated scope:** S
