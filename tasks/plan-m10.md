# Implementation Plan: Milestone 10 — Assessments and grades (API + MVC) + FCM grade-posted push

> **Plan approved 2026-11-10.** Built from [`SPEC.md`](../SPEC.md). Task details and acceptance criteria are in [`todo-m10.md`](./todo-m10.md). Milestone 8 stays in `plan.md` / `todo.md`, Milestone 9 in `plan-m9.md` / `todo-m9.md`.

## Context

Milestone 9 is committed (`ba5dae1`). Milestone 10 implements guide §27.9 #6 (weighted assessments, scores 0–MaxScore) and #7 (push when a grade is posted). The draft spec is in `SPEC.md`.

**Decisions made 2026-11-10:**
- Q1: weights never above 100%; grades allowed before the total reaches 100.
- Q2: push only on a student's first grade per assessment.
- Q3: push sent from an in-API background queue.
- Q5: rename the test project to `tests/Klasly.Tests`.
- Q4 (changed): the OAuth token for FCM is built **in-house, no NuGet package**, because `Google.Apis.Auth` 1.78.0 pulls in Newtonsoft.Json and System.Management 7.0.2. The plan includes **Firebase setup steps** for the team (no project exists yet).

**Outcome:**
- Teachers create assessments and enter a class's scores on one page.
- The API enforces every rule.
- A student's phone gets "New grade posted – ELECT3 – Quiz 1: 18 / 20".

No DB change: `Assessments`, `Grades`, `Notifications`, `Users.FcmToken` already exist with their CHECKs and unique indexes.

## Reuse (don't rebuild)

- **Ownership:** `SessionService.EnsureOwnClassAsync` / `TeacherIdAsync` (`src/Klasly.API/Services/SessionService.cs`). Copy the same small private helpers into the new services, following the existing per-service pattern.
- **Errors:** the service exceptions are mapped in `src/Klasly.API/Middleware/ApiExceptionHandler.cs` (404/400/409/403, plus a 2601/2627 backstop → 409). Add **one** case for row errors (below).
- **Race handling:** catch `DbUpdateException` with 2601/2627, as in `AttendanceRepository.TryAddAsync` / `TrySaveAsync`.
- **Active roster:** `IEnrollmentRepository.ListByClassAsync` (sorted by name, includes Student.User); filter `Status == Active`.
- **MVC:**
  - typed API repositories via the `AddHttpClient<TRepository,TImpl>` helper in `src/Klasly.Web/Program.cs`;
  - `HttpClient` `GetApiAsync` / `PostApiAsync` / `PutApiAsync` (`Infrastructure/ApiResponse.cs`);
  - `ApiException.IsFormError`;
  - `ModelStateExtensions.AddApiErrors`;
  - the `TempData["Success"/"Error"]` + `_Alerts.cshtml` pattern;
  - `ManilaTime`;
  - the `Teacher` area layout and breadcrumbs (`Areas/Teacher/Views/Classes/Details.cshtml`).
- **Android:**
  - `KlaslyRepository.call(...)` (never throws; 401 handling);
  - `KlaslyApi`;
  - `AppContainer` (the hand-built container with the saved `Session`);
  - `LoginViewModel`'s Student branch (where `registerDevice` runs) for token registration.
- **Tests:** `tests/Klasly.Worker.Tests/DatabaseFixture.cs` (LocalDB `Klasly_Tests`, real migrations) and the per-test own-class helper pattern in `AttendanceFinalizerTests.cs`.

## Design notes

- **Row validation errors:** a new `RowValidationException(IDictionary<string,string[]> errors)` in `Klasly.API/Services/`. The handler writes `ValidationProblemDetails` (400, `errors` keyed `grades[2].score`). The MVC `ApiException.Errors` already parses `errors`.
- **Weights:** `sum(other assessments' WeightPercent) + new ≤ 100`, computed in the service (one SUM query in the repo). Edit excludes the assessment itself.
- **Grade save:** load the assessment, active student ids and existing grades, then validate **all** rows first (score 0–MaxScore, ≤ 2 decimals, active student, no duplicate studentId in the request; blank = skip). Then, in one `SaveChanges`:
  - inserts (`PostedAt = now`) and updates (`Score`, `UpdatedAt = now` only when changed);
  - `DatePosted ??= now`;
  - one `Notification` per **new** grade (`Push`, `GradePosted`, `Pending`, `DedupKey = grade:{AssessmentId}:{StudentId}`, Subject "New grade posted", Body "{SubjectCode} – {Title}: {score} / {max}", using `0.##` formatting).
  
  A unique violation → `409` "Grades were just updated. Reload and try again." After the save, enqueue the new notification ids.
- **Push sending (`Klasly.API/Push/`):**
  - `PushQueue`: a singleton wrapping `Channel<int>`.
  - `PushSender : BackgroundService`:
    - on start, enqueue every `Pending`/`Push` row;
    - then, for each id, in a new DI scope: load the notification + user, check the token and configuration, call `IFcmPushClient.SendAsync`, set `Accepted` + `ExternalMessageId` or `Failed` + `LastError`, and `Attempts++`;
    - never throws out of the loop.
  - Missing configuration → `Failed` "FCM is not configured". No token → `Failed` "No device registered for push".
  - `shortcut:` there are no retries, and a stale (UNREGISTERED) token is not cleared. Upgrade if pushes go missing in practice.
- **`FcmPushClient`** (typed `HttpClient`, guide §13):
  - reads the service-account JSON at `Fcm:ServiceAccountPath` (user-secrets) with System.Text.Json (`project_id`, `client_email`, `private_key`, `token_uri`);
  - builds an RS256 JWT (`RSA.ImportFromPem`, scope `https://www.googleapis.com/auth/firebase.messaging`, 1 h);
  - POSTs it to `token_uri` (`grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer`) and caches the access token until 5 min before expiry;
  - sends `POST https://fcm.googleapis.com/v1/projects/{project_id}/messages:send` with `{ message: { token, notification: { title, body } } }` and returns the `name` or the error text.
  
  The token logic is about 30 lines with no package; it gets one unit test with a generated RSA key that checks the JWT's header, claims and signature.
- **FCM token endpoint:** `PUT api/students/me/fcm-token` (`StudentsController`, `StudentOnly`, `[Required, MaxLength(500)]`).
  - `IUserRepository.SetFcmTokenAsync(userId, token)` clears the same token on other users, then sets it on the caller, in one save. Returns 204.
- **Android Firebase without a project yet:**
  - in `app/build.gradle.kts`, apply `com.google.gms.google-services` **only if `app/google-services.json` exists**, so `assembleDebug` keeps passing;
  - at runtime, skip all Firebase calls when `FirebaseApp.getApps(context).isEmpty()`;
  - add `android/KlaslyApp/app/google-services.json` to `.gitignore`.
- **Versions** (confirmed 2026-11-10): Firebase BoM **35.1.0** (+ `firebase-messaging`, no version), Google services plugin **4.5.0**. No new NuGet package.

## Tasks

All of these are saved to `tasks/plan-m10.md` + `tasks/todo-m10.md` in Task 0. Each task ends with `dotnet build Klasly.slnx` + `dotnet test --solution Klasly.slnx` (Android tasks: `./gradlew assembleDebug testDebugUnitTest`).

0. **Housekeeping:** mark SPEC.md **Approved** with the answers above (Q4 changed to no package, plus the Firebase setup steps); write `tasks/plan-m10.md` and `tasks/todo-m10.md`; add `google-services.json` to `.gitignore`.
1. **Rename the test project:** `git mv tests/Klasly.Worker.Tests tests/Klasly.Tests`; update the namespace, `Klasly.slnx` and the csproj (+ a `Klasly.API` reference). The 11 tests still pass.
2. **Assessments API (test first):**
   - `AssessmentRequest` / `AssessmentResponse` DTOs, `IAssessmentRepository` + EF repo (list by class with graded/active counts, get tracked, weight sum, title exists, max posted score);
   - `AssessmentService` (create/edit/get/list, ownership, weight ≤ 100, title 409, max ≥ highest posted score);
   - `AssessmentsController` (GET/POST `api/classes/{classId}/assessments`, GET/PUT `api/assessments/{id}`);
   - register in DI;
   - tests: exactly 100 OK, 100.01 refused, edit excludes itself, max below posted score refused, other teacher 403.
3. **Grades API (test first):**
   - `GradeRow`, `SaveGradesRequest/Response`, `IGradeRepository`, `INotificationRepository.Add`, `RowValidationException` + handler case;
   - `GradeService.GetRosterAsync` / `SaveGradesAsync` as above;
   - `GET` / `PUT api/assessments/{id}/grades`;
   - tests:
     - 0 and MaxScore OK; −0.01, MaxScore + 0.01 and 3 decimals refused;
     - one bad row saves nothing;
     - a new grade gives exactly one Pending push row, a changed score none, the same score nothing;
     - a dropped student refused;
     - `DatePosted` set once.
   - **Checkpoint A:** smoke steps 1–6 through Swagger/curl.
4. **Push sender (test first):** `IFcmPushClient` + `FcmPushClient` (token + send), `PushQueue`, `PushSender` hosted service, `Fcm` options, `AddHttpClient`, and `GradeService` enqueueing after the save.
   - Tests with a fake client: Accepted + `ExternalMessageId`; error → Failed + `LastError`; not configured → Failed; no token → Failed; startup picks up Pending rows.
   - JWT unit test.
5. **FCM token endpoint (test first):** `FcmTokenRequest`, `IUserRepository.SetFcmTokenAsync`, `StudentsController` action. Tests: set; the same token moves from user A to B; 400 on empty.
   - **Checkpoint B:** API complete; smoke step 7 ("FCM is not configured" rows, grades unaffected).
6. **MVC class page — Assessments section:**
   - Web DTOs, `IAssessmentRepository` (API-backed), service methods, `AssessmentFormViewModel`;
   - the Classes/Details section (table + "Weights: X of 100%" + warning) and the **Add assessment** form posting to `Teacher/Assessments/Create`;
   - API errors on the form via `AddApiErrors`.
7. **MVC assessment page:** `Teacher/Assessments/Details/{id}`:
   - an edit form (`Edit` POST);
   - a grade roster with one number input per active student (`step=0.01`, `min=0`, `max=MaxScore`) and **Save grades** (`SaveGrades` POST);
   - `errors["grades[i].score"]` mapped back onto row i;
   - success "Grades saved: {added} new, {changed} changed."
8. **Android push receiver:**
   - Gradle: BoM 35.1.0, `firebase-messaging`, the plugin applied conditionally;
   - `KlaslyApi.setFcmToken` + repository method;
   - `push/KlaslyMessagingService` (`onMessageReceived` → notification channel "Grades" + `NotificationCompat`; `onNewToken` → send it if a Student session is saved);
   - after a Student login, get the token and send it, and ask for `POST_NOTIFICATIONS` once (Android 13+);
   - manifest: the service + the permission;
   - guard `FirebaseApp.getApps().isEmpty()`.
9. **Firebase setup (the team does this, with written steps in the HOW-TO):**
   1. create a Firebase project;
   2. add the Android app `com.klasly.app`, download `google-services.json` → `android/KlaslyApp/app/`;
   3. Project settings → Service accounts → generate a key → save it outside the repo;
   4. `dotnet user-secrets set "Fcm:ServiceAccountPath" "<path>" --project src/Klasly.API`.
   
   Then smoke steps 8–9 on an emulator with Google Play, or a phone.
10. **Finish:**
    - `/impeccable` on the Assessments section and the assessment page;
    - all smoke steps;
    - `dotnet ef migrations has-pending-model-changes` (none);
    - `dotnet list package` (Microsoft.* 10.x, no new NuGet);
    - confirm no Firebase files are tracked by git;
    - the three reports + an INDEX line.

Tasks 2–7 don't need Firebase. Task 9 is the only one blocked on the team.

## Critical files

- **API:**
  - new: `Services/AssessmentService.cs`, `Services/GradeService.cs`, `Services/RowValidationException.cs`, `Push/{FcmPushClient,PushQueue,PushSender}.cs`, `Controllers/AssessmentsController.cs`;
  - modified: `Controllers/StudentsController.cs`, `Middleware/ApiExceptionHandler.cs`, `Program.cs`.
- **Core / Infrastructure:**
  - new DTOs in `src/Klasly.Core/DTOs/`;
  - `Interfaces/{IAssessmentRepository,IGradeRepository,INotificationRepository}.cs` + `IUserRepository` (+1 method);
  - repositories in `src/Klasly.Infrastructure/Repositories/`;
  - `DependencyInjection.cs`.
- **Web:** `Areas/Teacher/Controllers/AssessmentsController.cs`, `Areas/Teacher/Views/Assessments/Details.cshtml`, `Areas/Teacher/Views/Classes/Details.cshtml`, plus repositories, services, `Models/Api` and view models.
- **Android:** `app/build.gradle.kts`, `gradle/libs.versions.toml`, `AndroidManifest.xml`, `data/{KlaslyApi,KlaslyRepository}.kt`, `ui/login/LoginViewModel.kt`, new `push/KlaslyMessagingService.kt`.
- **Tests:** `tests/Klasly.Tests/` (renamed) + `AssessmentServiceTests.cs`, `GradeServiceTests.cs`, `PushSenderTests.cs`, `FcmTokenTests.cs`.

## Verification

- `dotnet build Klasly.slnx` (0 warnings) and `dotnet test --solution Klasly.slnx` (the 11 existing tests + the new ones) after every task.
- `./gradlew assembleDebug testDebugUnitTest`, both **without** `google-services.json` (must build) and after Task 9.
- **Smoke test** (SPEC.md steps 1–10), with the API + website from the preview pane (`.claude/launch.json` `api` / `web`), curl/Swagger for API checks, and the browser pane for MVC at 375/800/1280 px:
  - assessments and the weight cap;
  - a bulk grade save with row errors;
  - a changed score creates no second push row;
  - max-score edit refused;
  - another teacher 403;
  - `Notifications` rows `Failed` "FCM is not configured" before Task 9 and `Accepted` after;
  - the real notification on a device;
  - the token moving between two students.
- `dotnet ef migrations has-pending-model-changes` → none. `git ls-files | grep -i "google-services\|service-account"` → empty.
