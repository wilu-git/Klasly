# Milestone 15 — Findings list (review of the whole solution)

> Spec: [`SPEC.md`](../SPEC.md) · Plan: [`plan-m15.md`](./plan-m15.md) · Tasks: [`todo-m15.md`](./todo-m15.md)
> Reviewed 2026-11-11 on `dev` at `867d40a`. Every outcome points to a commit or gives a reason.
> **Checkpoint A decision:** the team said to "continue until all of the tasks are done", so the spec's severity table decides on its own:
> - Critical and High are always fixed;
> - Medium is fixed when the change is small (≤ ~30 lines, no new package, no schema or contract change), otherwise deferred with a reason;
> - Low is logged only.

## Checklist used for every area

- **Rules:** guide §25 Final checklist; `CLAUDE.md` §3.x for the project, plus §4 and §4.1 (.NET 10 / C# 14 style, no `[Obsolete]` APIs).
- **Security (§27.5):**
  - ownership is checked on the server;
  - passwords are hashed;
  - no secrets in tracked files or history;
  - CORS is off;
  - every MVC POST has anti-forgery;
  - the auth cookie is HttpOnly + Secure + SameSite;
  - no sensitive data in logs (§16);
  - cleartext traffic in the Android debug build only.
- **Business rules:** §27.8 and §27.9, traced to code and a test (tables below).
- **Schema:** §27.7, from `dotnet ef migrations script` compared with `docs/database/Klasly_Schema.sql`.
- **Five axes:** correctness, readability, architecture (§2 layer flow), security, performance.
- **Bloat:** dead code, unused files and packages.

## Severity

| Severity | Meaning | Action in M15 |
|---|---|---|
| Critical | Security hole, data loss, or a broken Core feature | Fix + a test that fails before the fix |
| High | Breaks a guide/`CLAUDE.md` rule, or a wrong status code or message | Fix (+ test) |
| Medium | Real problem that does not block the demo | Fix if small, otherwise log with a reason |
| Low | Style, naming, a comment, an accepted limit | Log only |

## Baseline (before any change)

| Check | Result |
|---|---|
| `dotnet build Klasly.slnx --no-incremental` | **0 warnings, 0 errors** (all 6 projects) |
| `dotnet test Klasly.slnx` | **154 / 154 passed**, 0 failed, 0 skipped |
| `./gradlew assembleDebug testDebugUnitTest lintDebug` (`JAVA_HOME` = Android Studio `jbr`) | BUILD SUCCESSFUL; unit tests **11 / 11** (ApiTextTest 5, DisplayTextTest 6); lint **0 errors, 17 warnings** |
| `dotnet list package` | All Microsoft packages **10.0.14**. Third-party: Hangfire 1.8.25, Swashbuckle.AspNetCore.SwaggerUI 10.3.1, QRCoder 1.8.1, xunit.v3 4.0.3 |
| `--vulnerable --include-transitive` / `--deprecated` | **None** in any project |
| Secrets scan (tracked files + `git log -p --all`) | **Clean.** Only placeholders in docs (`xkeysib-...your key...`, `xkeysib-wrong`) and the `private_key` JSON property name in `FcmCredentials.cs`. No `Password=` in any connection string (Windows auth only). `local.properties` and `google-services.json` are git-ignored and were never committed. |
| Migrations | One: `20261109074245_InitialCreate` |

## Schema comparison (§27.7) — matches

`dotnet ef migrations script` was compared with `Klasly_Schema.sql`, table by table. Everything matches:
- all 17 tables, with the same column types and lengths;
- every CHECK (roles, statuses, YearLevel 1–6, Units 1–10, EndTime > StartTime, LateAfterMinutes 0–120, MaxScore > 0, WeightPercent (0, 100], Score ≥ 0, AbsenceRate 0–1, WeightedAverage 0–100, Attempts ≥ 0, ScannedAt when Source = Scan, term EndDate > StartDate);
- every unique rule, and `UX_AcademicTerms_OneActive … WHERE IsActive = 1`;
- no `ON DELETE CASCADE` anywhere;
- `SYSUTCDATETIME()` defaults on `datetime2(0)`;
- the 5 listed indexes;
- the 5 `SystemSettings` seed rows.

Three cosmetic differences, which are not findings because the schema behaves the same:
- the unique rules are unique **indexes** instead of unique constraints;
- default constraints have generated names instead of `DF_…`;
- EF adds an index on each FK column.

The seed data is fictional: `@klasly.test` addresses and `+63917000000x` numbers (RA 10173).

## Findings

| ID | Sev | Where | Rule | Finding | Outcome |
|---|---|---|---|---|---|
| F-01 | High | `src/Klasly.API/Program.cs`, `src/Klasly.Worker/Program.cs` | CLAUDE.md §3.3, guide §16 | Serilog is not installed. Both hosts use the default console logger. | **fixed** `512493f` (Task 7) |
| F-02 | High | `src/Klasly.Infrastructure/Repositories/UserRepository.cs:21`, `src/Klasly.API/Program.cs:58` | §27.5, §27.7 (soft delete) | A deactivated account keeps full API access until its JWT expires (≤ 60 min). Only the scan checks `IsActive`. A deactivated teacher could still post grades, and a deactivated parent could still read a child's overview. *(security-auditor, verified)* | **fixed** `cf32c74`: `OnTokenValidated` refuses a token whose account is inactive → 401 "Session expired – please log in"; test `LoginTests.Deactivated_account_loses_access_at_once_with_its_old_token` failed before (200) |
| F-03 | Medium | `src/Klasly.API/Services/AuthService.cs:19` | `AuthController.cs:23` ("so emails can't be probed") | An unknown or inactive email returns before the password hash check runs, so the response time shows which emails exist. *(security-auditor, verified)* | **fixed** `cf32c74`: hashes against a dummy hash when the email is unknown or inactive |
| F-04 | Medium | `src/Klasly.API/Controllers/AuthController.cs:14` | §27.5 | No rate limit or lockout on `POST /api/auth/login`. *(security-auditor)* | **deferred:** a limit per email needs the request body read before model binding. A limit per IP would throttle the MVC server, which logs every web user in from one IP. The demo runs on a private hotspot. Upgrade path: a failed-attempt counter + lockout on `Users` (a schema change, so ask first). |
| F-05 | Medium | `src/Klasly.API/Properties/launchSettings.json` (`lan`) | CLAUDE.md §3.3 (Swagger in Development only) | The `lan` profile serves Development (Swagger UI) over plain HTTP on `0.0.0.0`. *(security-auditor)* | **won't fix:** accepted in the M14 plan (risk table) for a demo on the team's own hotspot. HTTPS on the LAN needs a trusted certificate on every phone. The report notes it as a known limit. |
| F-06 | Medium | `android/KlaslyApp/app/src/main/res/xml/backup_rules.xml`, `data_extraction_rules.xml` | §3.6 (JWT in EncryptedSharedPreferences) | The backup rules are still the template stubs, so the encrypted session file `klasly_session.xml` goes into cloud backups and device transfers. Its Keystore key can't follow it there, so a restored copy can never be read (the app already recovers from that by deleting it). | **fixed** `27a506b` |
| F-07 | Medium | `tests/Klasly.Tests` | §27.9 FR1 traceability | Login failures are not tested over HTTP: a wrong password and an inactive account should both get 401 "Invalid email or password.". | **fixed** `cf32c74` (`LoginTests`, 2 tests) |
| F-08 | Medium | `tests/Klasly.Tests` | §27.9 FR2, §27.7 soft delete | Admin rules are only covered by the role sweep: one active term, an enrollment drop staying a row (Dropped) with re-enroll reusing it, a deactivated account staying listed, and no self-deactivation. | **fixed** `823d725` (`AdminRulesTests` 3, `TermActivationTests` 1, runs alone) |
| F-09 | Medium | `tests/Klasly.Tests` | §27.10 item 1, FR3 | The vertical slice (teacher opens → QR from the API → student registers phone and scans → teacher's list shows it) has no single HTTP test. Each piece is tested separately. | **fixed** `823d725` (`VerticalSliceTests`) |
| F-10 | Low | `src/Klasly.API/Services/AccountService.cs:117`, `UserConfiguration.cs:22` | §27.8 check 6 | `RegisteredDeviceId` is not unique, so one phone can be registered to several students. *(security-auditor)* | **deferred:** needs a filtered unique index (a schema change, so ask first). §27.8 already lists device binding as a mitigation, not a guarantee. |
| F-11 | Low | `src/Klasly.Infrastructure/Repositories/UserRepository.cs:33` | — | A student holding a classmate's FCM token could take over that classmate's grade pushes. *(security-auditor)* | won't fix: needs physical access to the other phone. |
| F-12 | Low | `src/Klasly.Core/DTOs/GradeDtos.cs` | — | `SaveGradesRequest.Grades` has no maximum row count (Teacher-only, capped by Kestrel's 30 MB body limit). *(security-auditor)* | logged |
| F-13 | Low | `src/Klasly.API/Middleware/ApiExceptionHandler.cs:32,37` | guide §15 | Any framework `KeyNotFoundException` / `UnauthorizedAccessException` is mapped to 404/401 with its message. No current request path throws one. *(security-auditor)* | logged |
| F-14 | Low | `src/Klasly.API/Program.cs:77` | — | `ValidAlgorithms` is not pinned to HS256. The symmetric key already rules out other key types. *(security-auditor)* | logged |
| F-15 | Low | `src/Klasly.Infrastructure/Repositories/UserRepository.cs:73` | §27.7 soft delete | Unlinking a parent from a student deletes the `StudentGuardians` row. | won't fix: approved earlier as "the one hard delete in Klasly" (`IUserRepository.RemoveLinkAsync`). The join table has no status column and nothing references the row. |
| F-16 | Low | `src/Klasly.Infrastructure/Data/KlaslySeed.cs` | §27.5 | Every seeded account shares the known password `Klasly@123`. | won't fix: fictional demo data. Change the passwords (or skip the seed) before any real deployment. |
| F-17 | Low | developer machine | CLAUDE.md §3.2 | The `dotnet-ef` tool is 10.0.11, older than the 10.0.14 runtime (it still prints a warning). | logged: `dotnet tool update --global dotnet-ef` |
| F-18 | Low | `src/Klasly.Web/Areas/**/Controllers/*.cs` | — | Read-only actions have no explicit `[HttpGet]`. Every state change is POST with `[ValidateAntiForgeryToken]`. | logged |
| F-19 | Low | `android/KlaslyApp` (lint) | — | 17 lint warnings: UnusedResources ×7 (`colors.xml`), NewerVersionAvailable ×3, GradleDependency ×2, AndroidGradlePluginVersion, OldTargetApi, ObsoleteSdkInt, UseKtx, RedundantLabel. | logged |
| F-20 | Low | `src/Klasly.Core/Interfaces/IUserRepository.cs:19` | — | `AddAsync`'s doc comment sat on `SetFcmTokenAsync` (two `<summary>` tags). | fixed in passing, `cf32c74` (the file was already being changed for F-02) |

### Accepted limits (`shortcut:` comments)

Each `shortcut:` comment in the code was reviewed. All of them are still fine for the demo:

| ID | Where | Limit | Outcome |
|---|---|---|---|
| S-1 | `src/Klasly.API/Push/PushNotificationSender.cs:8` | One push attempt, and a stale FCM token is not cleared | keep |
| S-2 | `src/Klasly.API/Services/AccountService.cs:108` | Two phones registering at the same instant: last write wins | keep (see F-10) |
| S-3 | `src/Klasly.API/Services/AccountService.cs:203` | UTC is stamped per response instead of by a global converter | keep |
| S-4 | `src/Klasly.API/Services/NotificationService.cs:9` | Failed emails: newest 100, no paging | keep |
| S-5 | `src/Klasly.Web/Services/ClassService.cs:56` | Inactive teachers are left out of the class form | keep |
| S-6 | `src/Klasly.Worker/Services/EmailSender.cs:16` | 50 emails per tick (fixed) | keep |
| S-7 | `android/…/data/TokenStore.kt:48` | `security-crypto` is deprecated | Low, won't fix before the demo (Q3) |
| S-8 | `android/…/push/KlaslyMessagingService.kt:23` | `onNewToken` is deprecated | Low, won't fix before the demo (Q3) |
| S-9 | `android/…/push/PushRegistration.kt:37` | `getToken` is deprecated | Low, won't fix before the demo (Q3) |

## Per-area notes (what was checked and found OK)

- **Core + Infrastructure:**
  - `Klasly.Core` has no packages;
  - repositories hold data access only;
  - `AsNoTracking` on read lists, and tracked reads only where a save follows;
  - risk inputs are loaded in one command (tested);
  - async + `CancellationToken` throughout;
  - no `ExecuteDelete`, and the only `Remove` is F-15;
  - unique-key races map to 409.
- **API:**
  - every controller has the right policy, plus the fallback policy;
  - ownership is checked in the services (sessions, attendance, assessments, grades, parent overview), and `MarkedBy` comes from the token;
  - DTOs only;
  - all 7 scan checks run in the guide's order with its exact messages;
  - the HMAC is compared with `FixedTimeEquals`, with a per-session 32-byte nonce;
  - Identity `PasswordHasher`;
  - CORS is off;
  - Swagger in Development only;
  - problem-details with no stack traces;
  - logs hold ids and statuses only.
- **Worker:**
  - Hangfire with SQL Server storage;
  - `worker-tick` registered with `AddOrUpdate`, with the cron from `Worker:Cron`;
  - `[DisableConcurrentExecution(600)]` and `[AutomaticRetry(Attempts = 0)]`;
  - no web host or dashboard;
  - the job only calls the 5 services, in order;
  - the thresholds come from `SystemSettings` and are range-checked;
  - risk follows §27.8 (Excused and Cancelled are excluded; NULL average until a grade exists);
  - DedupKeys are used, and alerts go only to guardians with `ReceivesAlerts = 1`;
  - Brevo: 10 s timeout, HTTPS-only base URL, api-key from configuration, the 2xx / permanent / transient / unauthorized split, backoff 5·2ⁿ min + 0–60 s jitter, `MaxEmailAttempts`;
  - no guardian email or key is logged.
- **Web:**
  - no Core or Infrastructure reference and no EF package, only `QRCoder`;
  - typed HttpClients with `BearerTokenHandler`;
  - the JWT sits inside an encrypted, HttpOnly, `Secure=Always`, `SameSite=Lax` cookie that is non-persistent and ends with the JWT;
  - all 19 POST actions have `[ValidateAntiForgeryToken]`;
  - all 10 area controllers have `[Authorize(Roles)]`;
  - `LocalRedirect` is used for the return URL;
  - the API's messages are shown through `ApiExceptionFilter` and the forms;
  - the QR page polls the MVC `Live` action, not the API;
  - the only `Html.Raw` renders a QR SVG the server makes itself;
  - no thresholds in the views.
- **Android:**
  - MVVM with `StateFlow`;
  - Loading / Error / Empty / Ready on every overview screen, and the scan shows the API's message;
  - the JWT is in EncryptedSharedPreferences, with no logging interceptor;
  - `INTERNET` and `CAMERA` permissions;
  - cleartext only through `src/debug/AndroidManifest.xml`;
  - the API URL comes from git-ignored `local.properties`;
  - a 401 logs the user out with the API's message.
- **Tests:**
  - every test runs on `Klasly_Tests` (a fixture assertion guards this);
  - no skipped tests;
  - the assertions check exact statuses and messages.

## Traceability — §27.9 functional requirements

| FR | Requirement | Code | Tests |
|---|---|---|---|
| 1 | Log in with 4 roles; access restricted by role and ownership | `AuthService`, policies + fallback, `OnTokenValidated` active check, `*Service` ownership checks | `ApiFactoryTests`, `RoleAccessTests` (5), `OwnershipTests` (3), `LoginTests` (3) |
| 2 | Admin manages terms, subjects, classes, enrollments | `TermService`, `SubjectService`, `ClassService`, `EnrollmentService`, `AccountService` | `RoleAccessTests` (own role passes), `AdminRulesTests` (3), `TermActivationTests` (1) |
| 3 | Signed QR that refreshes every 30 s | `QrTokenService`, `SessionService.GetQrAsync`, MVC `_Live` poll | `ScanTests` (signature, step, grace), `VerticalSliceTests` (QR endpoint, 30 s step, scan, teacher's list) |
| 4 | Present/Late on a valid scan; clear refusals | `AttendanceService.ScanAsync` | `ScanTests` (7 checks + late cut-off), `OwnershipTests`, `RoleAccessTests` (scan 401) |
| 5 | Auto-absent after the session closes | `AttendanceFinalizer` | `AttendanceFinalizerTests` (10) |
| 6 | Weighted assessments; score 0–max | `AssessmentService` | `AssessmentServiceTests` (7), `GradeTests` (6) |
| 7 | Push when a grade is posted | `AssessmentService` → `PushQueue` → `PushNotificationSender` | `GradeTests`, `PushTests` (7) |
| 8 | At-risk at ≥ 20% absent or < 75 average | `RiskRules`, `RiskCalculator`, `RiskRepository` | `RiskRulesTests`, `RiskCalculatorTests` (exactly 20% / 75), `RiskRepositoryTests` |
| 9 | Brevo alerts, retries, never twice | `AlertQueuer`, `EmailSender`, `BrevoEmailClient` | `AlertQueuerTests` (13), `EmailSenderTests` (10), `BrevoEmailClientTests` (10) |
| 10 | Clear error messages | `ApiExceptionHandler`, MVC `ApiExceptionFilter`, Android `errorMessage` | exact messages in `ScanTests` / `OwnershipTests`, `ApiTextTest` (Android) |

## Traceability — §27.8 scan checks

| # | Check | Status + message | Test |
|---|---|---|---|
| 1 | JWT + role Student | 401 "Session expired – please log in" | `RoleAccessTests.Scan_by_a_non_student_is_401_session_expired` |
| 2 | HMAC signature | 400 "Invalid QR code" | `ScanTests.Bad_signature_is_400_invalid_qr` |
| 3 | Current or previous step | 400 "QR expired – rescan" | `ScanTests.Qr_two_steps_old_is_400_expired`, `Qr_one_step_old_is_still_accepted` |
| 4 | Session Open | 409 "Session is closed" | `ScanTests.Session_that_is_not_open_is_409_closed` |
| 5 | Enrolled | 403 "Not enrolled in this class" | `ScanTests.Dropped_student_is_403_not_enrolled`, `OwnershipTests.Student_cannot_scan_a_class_they_are_not_enrolled_in` |
| 6 | Registered device | 403 "Unregistered device" | `ScanTests.Another_phone_is_403_unregistered_device` |
| 7 | Not already checked in | 409 "Already checked in" | `ScanTests.Second_scan_is_409_already_checked_in` |
| — | Late cut-off | Present / Late | `ScanTests.Scan_at_the_late_cut_off` (±1 min) |

## After the fixes (Checkpoint B)

| Check | Baseline | Now |
|---|---|---|
| `dotnet build` warnings | 0 | **0** |
| `dotnet test` | 154 / 154 | **162 / 162** (run twice) |
| Android `assembleDebug testDebugUnitTest lintDebug` | OK, 11 / 11, lint 17 warnings / 0 errors | **OK, 11 / 11, lint 17 warnings / 0 errors** |
| Microsoft packages | 10.0.14 | **10.0.14** (no `Microsoft.AspNetCore.*`, `EntityFrameworkCore.*` or `Extensions.*` below 10.x, direct or transitive) |
| New packages | — | Serilog.AspNetCore 10.0.0; Serilog.Extensions.Hosting 10.0.0, Serilog.Settings.Configuration 10.0.1, Serilog.Sinks.Console 6.1.1 |
| `--vulnerable --include-transitive` | none | **none** |

Fix groups:
- **Task 8** (Core/Infrastructure/API): `cf32c74`.
- **Task 9** (Worker): nothing to fix. Every Worker item passed or is Low.
- **Task 10** (Web): nothing to fix. F-18 is Low.
- **Task 11** (Android + test gaps): `27a506b`, `823d725`.

Every row above now has an outcome.
