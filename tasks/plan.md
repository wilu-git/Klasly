# Implementation Plan: Milestone 6 — Web API sessions, rotating QR token, scan check-in

> Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-09: Q1, Q3, Q4 as recommended; Q2 changed to a separate device endpoint). Task details, acceptance criteria and verification steps are in [`todo.md`](./todo.md).
> Milestone 5's plan is in git history (commit `f859150`).

## Overview

Build the server half of the QR check-in. A teacher creates and opens a session for their own class. The API hands out a signed token that rotates every `QrStepSeconds` (30 s). A student's phone, registered right after login, sends the token back and is recorded Present or Late. The work is **Web API only**: no database change, no new package, no MVC or Android code.

## Facts from the code that shape the plan

- Services throw and `ApiExceptionHandler` maps the exception to a status code (`KeyNotFoundException` → 404, `ValidationException` → 400, `ConflictException` → 409, unique-index `DbUpdateException` → 409). Milestone 6 adds `ForbiddenException` → 403 and `UnauthorizedAccessException` → 401.
- Controllers read the caller with `User.FindFirstValue("sub")` (`MapInboundClaims = false`).
- `IUserRepository.GetByIdAsync` returns a **tracked** user with Student / Teacher / Guardian loaded. It is enough for teacher ownership, device registration and scan check 1.
- `IEnrollmentRepository.GetByClassAndStudentAsync` serves check 5, and `ListByClassAsync` serves the roster.
- `IClassRepository.ListAsync` / `GetDetailsAsync` load Subject, Teacher.User, Term and the Active enrollments, which is the shape `ClassResponse` needs for "my classes".
- `ClassSessions.QrNonce` (nvarchar 64), `CK_ClassSessions_Late` (0–120), unique `AttendanceRecords (SessionId, StudentId)`, `Users.RegisteredDeviceId` (nvarchar 200) and the `QrStepSeconds = 30` setting all exist already.
- SQL returns `DateTime` as `Unspecified`. Responses stamp UTC the same way `AccountService.Utc` does.

## Dependency graph

```
Task 1  Error plumbing + Qr:SigningKey check
   │
   └── Task 2  QR token service (+ settings repository)
          │
          └── Task 3  Create / read sessions, my classes   ← session repository, ownership
                 │
                 └── Task 4  Open / close / QR endpoint
                        │
Task 5  Device register / reset  (needs Task 1 only)
                        │
                        └── Task 6  Scan endpoint (needs 2, 4, 5)
                               │
                               └── Task 7  Roster (needs the attendance repository from 6)
                                      │
                                      └── Task 8  Smoke test, package check, reports, commit
```

All tasks touch `Program.cs` or `DependencyInjection.cs`, so they run one after another in one session.

## Architecture decisions

| Decision | Why |
|---|---|
| Token `{sessionId}.{timeStep}.{Base64Url(HMACSHA256(key, "{sessionId}\|{timeStep}\|{nonce}"))}`; accept the current step and the previous one; compare with `CryptographicOperations.FixedTimeEquals` | Guide §27.8 and spec assumption 3. |
| `QrTokenService` has no database code except reading `QrStepSeconds` through `ISystemSettingRepository` | The HMAC logic stays pure and easy to read. |
| Ownership: load the session with its Class, then compare `Class.TeacherId` with the caller's Teacher row. Unknown id → 404, someone else's class → 403 | Spec assumption 6. Admins never get past the `TeacherOnly` policy. |
| "Effectively open" = `Status == Open && UtcNow < EndTime`; used by the QR endpoint and scan check 4 | Decision Q4: no Worker yet. |
| Device registration in `AccountService` (`RegisterDeviceAsync`, `ResetDeviceAsync`) | `AccountService` already owns all `User` edits; no new service. |
| Scan check 1 uses plain `[Authorize]`, and the service throws `UnauthorizedAccessException` for a non-Student | The guide asks for 401 (not 403) on a wrong role. |
| JWT `OnChallenge` writes problem-details "Session expired – please log in" | The app can show the API's message; the website checks only the status code. |
| A lost insert race → `ConflictException("Already checked in")`, caught in `AttendanceService` | The generic "That value is already used." is wrong here. |

## Task list

### Phase 1: Foundation
- [x] Task 1: Error plumbing and secrets check (S)
- [ ] Task 2: QR token service (S–M)

### Checkpoint A — after Tasks 1–2
- [ ] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [ ] No token → 401 with the message; the API refuses to start without `Qr:SigningKey`

### Phase 2: Teacher session slice
- [ ] Task 3: Create and read sessions, list my classes (M)
- [ ] Task 4: Open, close and the QR endpoint (S–M)

### Checkpoint B — after Tasks 3–4
- [ ] SPEC smoke steps 1–3 pass

### Phase 3: Device, scan, roster
- [ ] Task 5: Register and reset a student's device (S)
- [ ] Task 6: Scan endpoint with the 7 checks (M)
- [ ] Task 7: Live attendance roster (S)

### Checkpoint C — after Tasks 5–7
- [ ] SPEC smoke steps 4–15 pass

### Phase 4: Wrap-up
- [ ] Task 8: Package check, reports, spec tick, commit (S)

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| HMAC or step code accepts a bad token (no unit tests) | High | Constant-time compare. Smoke steps 9–10 cover a forged token, an edited id, garbage text, an expired token and the grace step. Role and boundary tests come in Milestone 14. |
| Insert race gives a 500 or the generic message | Med | Catch 2601/2627 in the service; step 13 sends two curl calls at once. |
| The OnChallenge body changes the website's 401 handling | Low | The website's `ApiExceptionFilter` checks only the status code and `ApiResponse` already reads any JSON body; step 1 re-checks. |
| Two accounts register the same phone | Low | Known limit (the column isn't unique); written in the report. |
| Android ID differs between debug and release builds | Med | Report troubleshooting: after switching builds, the admin resets each test student once. |
| A student is stuck with a 409 on a new phone | Low | The 409 message says to ask the admin; the admin reset is idempotent. |

## Out of scope (from SPEC.md)

MVC QR page (Milestone 7), Android scanner (Milestone 8), Worker auto-close and Absent marking (Milestone 9), attendance corrections, cancel/edit session, GPS or offline scans, a test project, new packages, schema changes.

## Open questions

None. The four spec questions were decided on 2026-11-09 (SPEC.md "Decisions").
