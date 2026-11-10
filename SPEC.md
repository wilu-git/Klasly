# Spec: Milestone 10 — Assessments and grades (API + MVC), with the FCM grade-posted push

> Status: **Approved** (2026-11-10). Open questions 1, 2, 3 and 5 answered as recommended. **Q4 changed:** the FCM OAuth token is built in-house with .NET's RSA + System.Text.Json, with **no NuGet package** (`Google.Apis.Auth` would pull in Newtonsoft.Json and System.Management 7.0.2). Android uses Firebase BoM 35.1.0 + the google-services plugin 4.5.0. No Firebase project exists yet, so the plan includes setup steps for the team. Plan: `tasks/plan-m10.md` + `tasks/todo-m10.md`.
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.2 "Assessments & grade entry" and "Grade push notifications", §27.6 tables, §27.7 conventions, §27.8 "Grade-posted push", §27.9 #6–7, §13 `FcmPushClient`, §14 secrets) → `CLAUDE.md` (§3.3 Web API, §3.4 Web MVC, §3.6 Android) → this spec.
> Milestone 9's spec is in git history (commit `ba5dae1`) and in `docs/reports/2026-11-10-1811-milestone-9-worker-absent-corrections/`.

## What exists today

- **Tables are ready.** `Assessments`, `Grades`, `Notifications` and `Users.FcmToken` exist with every CHECK and unique index from `Klasly_Schema.sql`:
  - `MaxScore > 0`, `0 < WeightPercent ≤ 100`, `Score ≥ 0`;
  - unique `(ClassId, Title)` and `(AssessmentId, StudentId)`;
  - a unique `Notifications.DedupKey`.
  
  Nothing reads or writes them yet: no repository, service, endpoint or page.
- **Teacher side:**
  - the API has `TeacherOnly` session endpoints with an ownership check (`EnsureOwnClassAsync`);
  - MVC has **My classes → class page** (start session + sessions list) and the session page.
- **Android:**
  - login, phone registration and Scan QR screens;
  - no Firebase, no notifications;
  - no "My grades" screen (that's Milestone 13).
- **Tests:** `tests/Klasly.Worker.Tests` (xUnit v3, LocalDB `Klasly_Tests`) exists. It tests Worker services only.

## Assumptions (correct these before we build)

1. **No database change.** Every column exists. No migration.
2. **Assessments (API):** a teacher manages assessments only for their own classes.
   - **Fields:** `Title` (required, ≤ 100, unique in the class), `Type` (required, ≤ 30, free text; the page suggests Quiz, Exam, Activity, Project, Recitation), `MaxScore` (> 0, ≤ 9999.99, 2 decimals), `WeightPercent` (> 0, ≤ 100, 2 decimals).
   - **Weights rule:** the class total may never go above **100%** (open question 1). The class page shows "Weights: 60 of 100%" and a warning while the total is under 100.
   - **Edit:** title, type, max score and weight can be changed. A new `MaxScore` below a score already posted → `400` "Max score can't be lower than a posted score (18)."
   - **No delete** (the table has no soft-delete column; deleting a graded assessment would lose grades). Out of scope.
   - **`DatePosted`:** set to the time (UTC) when the first grade of that assessment is saved; null until then.
3. **Grades (API):** one bulk call per assessment saves the scores of many students at once.
   - **Who:** only **actively enrolled** students of the class. A student not actively enrolled → `400` for that row.
   - **Values:** each score is `0 ≤ score ≤ MaxScore`, 2 decimals at most. A blank score means "not graded yet" and is skipped.
   - **All or nothing:** one invalid row → `400` problem-details with field errors (`grades[2].score`), and nothing is saved.
   - **Writes:** a new grade → insert (`PostedAt = now`). A changed score → update `Score`, `UpdatedAt = now`. The same score → no change.
   - **No "un-post":** a posted grade can be changed but not removed. Out of scope.
   - **Response:** `200` with the assessment's roster (student, score or null, posted/updated time).
   - **Concurrency:** if two saves race to insert the same student's grade, the unique `(AssessmentId, StudentId)` index rejects one. The API turns that into `409` "Grades were just updated. Reload and try again.", the same pattern as attendance.
4. **Grade-posted push (API, guide §27.8):** when a grade is saved **for the first time** (open question 2):
   - the API writes a `Notifications` row in the **same save** as the grade: `UserId` = the student's user, `Channel = Push`, `Type = GradePosted`, `Status = Pending`, `DedupKey = grade:{AssessmentId}:{StudentId}`, `Subject` "New grade posted", `Body` "ELECT3 – Quiz 1: 18 / 20";
   - after the save, the push is sent **in the background** so the teacher never waits for Firebase (open question 3);
   - result: FCM `200` → `Accepted` with FCM's message name in `ExternalMessageId`; any error → `Failed` with `LastError`. No retries: a push is best-effort, and the grade is already saved and visible;
   - a student with no `FcmToken` → `Failed`, `LastError` "No device registered for push";
   - **Firebase not configured** (no credentials in user-secrets) → `Failed`, `LastError` "FCM is not configured". Grades still work, so the team can build and test everything except the real push before Firebase exists (open question 4).
5. **FCM sender (API, guide §13):**
   - a typed `HttpClient` `FcmPushClient` calls FCM HTTP v1 (`POST https://fcm.googleapis.com/v1/projects/{projectId}/messages:send`) with a Google OAuth access token from the service account;
   - credentials: the service-account JSON file path in user-secrets (`Fcm:ServiceAccountPath`), never in the repo;
   - one new package for the OAuth token (open question 4).
6. **FCM token (API + Android):**
   - **New endpoint:** `PUT api/students/me/fcm-token` with `{ "token": "…" }` (`StudentOnly`). It saves the token on the caller's `Users` row and **clears the same token from any other user**, so a shared phone never gets another student's grade.
   - **Android:**
     - after login, and whenever Firebase gives a new token (`onNewToken`), the app sends it;
     - on logout it does nothing extra (the next student's login moves the token);
     - a `FirebaseMessagingService` shows the push as a system notification ("New grade posted", "ELECT3 – Quiz 1: 18 / 20");
     - on Android 13+ the app asks for the notification permission once, after the first student login;
     - tapping the notification opens the app (the grade screen comes in Milestone 13).
   - Parents get no push in this milestone.
7. **MVC teacher pages** (`Teacher` area; `[ValidateAntiForgeryToken]` on every POST; API errors shown with the existing patterns):
   - **Class page:** a new **Assessments** section under Sessions. It lists title, type, max score, weight, graded "4 of 6", and a total "Weights: 60 of 100%" with a warning below 100. It also has an **Add assessment** form.
   - **Assessment page** (`Teacher/Assessments/Details/{id}`):
     - the assessment details with an **Edit** form;
     - the roster of active students (student no., name, a score input `0–MaxScore`, last posted/updated time in Manila);
     - one **Save grades** button;
     - success: "Grades saved: 3 new, 1 changed." Errors show on the matching rows.
8. **Privacy (RA 10173):** a push payload goes through Google's servers. It carries only the subject code, assessment title and score: no student name or number. Test data stays fictional.
9. **Out of scope:**
   - student/parent grade screens (Milestone 13);
   - risk and weighted average (Milestone 11);
   - email (Milestone 12);
   - deleting assessments or grades;
   - push retries;
   - a push for changed scores (open question 2);
   - importing grades from a file;
   - grade history.

## Objective

A teacher sets up a class's graded work (quizzes, exams, projects with weights totalling 100%) and enters scores for the whole class on one page. The API refuses impossible scores and weights. When a student's grade is posted, their phone shows a notification within seconds.

Functional requirements covered: guide §27.9 **#6** (weighted assessments, scores between 0 and the max) and **#7** (push when a grade is posted).

### User stories

- As a **teacher**, I can add an assessment to my class with a title, type, max score and weight, and I can't push the class total over 100%.
- As a **teacher**, I can enter or change the scores of all my students for an assessment on one page, and a score above the max is refused with a clear message.
- As a **teacher**, I can't see or change another teacher's assessments.
- As a **student**, my phone shows "New grade posted – ELECT3 – Quiz 1: 18 / 20" when my teacher posts my grade.
- As a **student** who shares a phone, I never get another student's grade notification.

## API contract (new)

| Call | Body | Success | Errors |
|---|---|---|---|
| `GET api/classes/{classId}/assessments` (Teacher) | — | `200 [ { assessmentId, title, type, maxScore, weightPercent, datePostedUtc, gradedCount, activeCount } ]` | `403` not own class · `404` |
| `POST api/classes/{classId}/assessments` (Teacher) | `{ title, type, maxScore, weightPercent }` | `201` the assessment | `400` validation / total over 100% · `403` · `404` · `409` title already used in the class |
| `GET api/assessments/{id}` (Teacher) | — | `200` the assessment | `403` · `404` |
| `PUT api/assessments/{id}` (Teacher) | same as POST | `200` the assessment | `400` (incl. max below a posted score) · `403` · `404` · `409` title |
| `GET api/assessments/{id}/grades` (Teacher) | — | `200 [ { studentId, studentNumber, fullName, score, postedAtUtc, updatedAtUtc } ]` active students by name | `403` · `404` |
| `PUT api/assessments/{id}/grades` (Teacher) | `{ "grades": [ { "studentId": 1, "score": 18.5 } ] }` | `200` the roster + `{ added, changed }` | `400` row errors · `403` · `404` · `409` race |
| `PUT api/students/me/fcm-token` (Student) | `{ "token": "…" }` | `204` | `400` empty / > 500 chars · `401` |

## Commands

```bash
dotnet build Klasly.slnx
dotnet test --solution Klasly.slnx
dotnet run --project src/Klasly.API --launch-profile https       # Swagger at /swagger
dotnet run --project src/Klasly.Web --launch-profile https
dotnet user-secrets set "Fcm:ServiceAccountPath" "C:\secrets\klasly-fcm.json" --project src/Klasly.API   # not committed
dotnet list package                                              # all Microsoft.* still 10.x
cd android/KlaslyApp; ./gradlew assembleDebug testDebugUnitTest
```

## Project structure (files this milestone touches)

```
src/Klasly.Core/
  DTOs/            AssessmentRequest, AssessmentResponse, GradeRow, SaveGradesRequest/Response, FcmTokenRequest   (new)
  Interfaces/      IAssessmentRepository, IGradeRepository, INotificationRepository (new); IUserRepository (+ token methods)
src/Klasly.Infrastructure/
  Repositories/    AssessmentRepository, GradeRepository, NotificationRepository (new); UserRepository (modified)
  DependencyInjection.cs                                                                                    (register)
src/Klasly.API/
  Controllers/     AssessmentsController (new); StudentsController (+ fcm-token)
  Services/        AssessmentService, GradeService (new); Push/FcmPushClient + PushSender background queue (new)
  Program.cs       typed HttpClient, background service, options
src/Klasly.Web/
  Areas/Teacher/   AssessmentsController + Views/Assessments/Details.cshtml (new); Classes/Details.cshtml (Assessments section)
  Repositories/, Services/, Models/Api/, ViewModels/   assessment + grade calls and view models
android/KlaslyApp/
  app/build.gradle.kts, gradle/libs.versions.toml     Firebase BoM + messaging, google-services plugin
  app/google-services.json                            from the team's Firebase project — NOT committed (open question 4)
  data/  KlaslyApi (+ fcm-token), KlaslyRepository
  push/  KlaslyMessagingService (new); AndroidManifest (service, POST_NOTIFICATIONS)
tests/Klasly.Worker.Tests → see Testing strategy
```

## Code style

The same as Milestones 3–9:
- controllers stay thin;
- rules live in services (`ValidationException` → 400, `ForbiddenException` → 403, `KeyNotFoundException` → 404, `ConflictException` → 409, via the existing `ApiExceptionHandler`);
- DTOs, never entities;
- `CancellationToken` all the way down.

```csharp
public async Task<SaveGradesResponse> SaveGradesAsync(int assessmentId, SaveGradesRequest request, int userId, CancellationToken ct)
{
    var assessment = await GetOwnAssessmentAsync(assessmentId, userId, ct);          // 404 / 403
    var errors = Validate(request.Grades, assessment.MaxScore, activeStudentIds);     // all rows first
    if (errors.Count > 0) throw new RowValidationException(errors);                  // 400, nothing saved
    // insert/update grades + one Pending push row per first-time grade, one SaveChanges
}
```

## Testing strategy

- **Automated:** the existing test project's LocalDB fixture is reused for the API's grade and assessment rules. A small `tests/Klasly.API.Tests` project, or the same project renamed `Klasly.Tests` (decided in `/plan`), covers:
  - weights: total exactly 100 OK, 100.01 refused, edit counts the other assessments only;
  - scores: 0 and exactly MaxScore OK; −0.01 and MaxScore + 0.01 refused; 3 decimals refused; one bad row saves nothing;
  - first grade → exactly one Pending push row; a changed score → no new push row; the same score again → nothing;
  - a lower MaxScore under a posted score refused;
  - the FCM token moves from user A to user B;
  - the push sender with a fake `FcmPushClient`: success → Accepted, error → Failed, not configured → Failed.
- **Manual smoke test** (API + website, LocalDB seed data; push steps need Firebase and a phone or emulator with Google Play):
  1. Teacher adds Quiz 1 (20 points, 30%) and Midterm (100 points, 70%) → "Weights: 100 of 100%". A third assessment at 10% → refused.
  2. Assessment page: enter scores for 4 of 6 students → "Grades saved: 4 new, 0 changed". The class page shows "4 of 6".
  3. A score of 21 on Quiz 1 → the row shows "Score must be between 0 and 20." and nothing is saved.
  4. Change one score → "0 new, 1 changed"; the DB shows `UpdatedAt`; `Notifications` has no second row for that student.
  5. Edit Quiz 1's max score to 15 when someone has 18 → refused with the message.
  6. Swagger as another teacher → 403 on the assessment and its grades.
  7. Without Firebase configured: the `Notifications` rows show `Failed` / "FCM is not configured", and grade saving still works.
  8. With Firebase: a student logged in on the emulator/phone gets "New grade posted – ELECT3 – Quiz 1: 18 / 20" within ~10 s; the row shows `Accepted`.
  9. A second student logs in on the same phone → the first student's `FcmToken` is cleared.
  10. `/impeccable` on the class page Assessments section and the assessment page.

## Boundaries

- **Always:**
  - ownership checked in the API for every assessment and grade call;
  - validation repeated in the API (MVC hints are not security);
  - UTC in the DB and Manila on screen;
  - build + tests after every change;
  - reports after every prompt.
- **Ask first:**
  - any new package (the OAuth/Firebase ones, see open question 4);
  - any schema change (none planned);
  - adding Firebase files to the repo;
  - sending real pushes to anyone but the team's test devices.
- **Never:**
  - commit `google-services.json`, the service-account JSON or any key;
  - put student names or numbers in push payloads;
  - let a push failure undo or block a saved grade;
  - add a Hangfire dashboard or move push sending into the Worker (the guide puts it in the Web API).

## Success criteria

- [x] A teacher can add and edit assessments for their own classes, and the class weight total can't go above 100% (exactly 100 allowed).
- [x] A teacher can save the scores of a whole class in one go. Scores outside 0–MaxScore or with more than 2 decimals are refused with a row message, and nothing partial is saved.
- [x] Another teacher gets 403; an unknown assessment gets 404; a race gets 409.
- [x] A student's first grade on an assessment creates exactly one `Push / GradePosted` Notification, sent in the background. It ends `Accepted` (Firebase configured) or `Failed` with a clear `LastError`, and grade saving never waits for or fails because of it.
- [ ] With Firebase configured, the Android app registers its FCM token and shows the grade notification. A shared phone moves the token to the last student who logged in.
- [x] No migration; `has-pending-model-changes` reports none; every Microsoft package still 10.x; no secrets or Firebase files committed.
- [ ] All smoke steps behave as written (step 8 only with Firebase); `/impeccable` run on the new pages; reports written.

## Open questions (my recommendation first)

1. **Weights rule:** what does "weights per class total 100%" mean while the teacher is still adding assessments?
   - **Recommended:** the total may **never go above 100%**. Grades can be posted while it's under 100 (the weighted average in Milestone 11 already divides by the weights of graded assessments only), and the page warns until the total is exactly 100.
   - *Alternative:* grades can't be posted until the class total is exactly 100%. Stricter, but the teacher must create every assessment of the term before entering the first quiz score.
2. **When does a push go out?**
   - **Recommended:** only when a student's grade is posted **for the first time** (`DedupKey grade:{AssessmentId}:{StudentId}`).
   - *Alternative:* also when a score is changed. That needs a different DedupKey per change and could spam students during a correction.
3. **How is the push sent "async"?**
   - **Recommended:** the grade save also writes the Pending Notification. A small in-API background service (a `Channel<int>` of notification ids, ~20 lines) sends it right after the request finishes, and on startup it picks up any `Pending` push rows a restart interrupted.
   - *Alternative:* send inline after the save with a 5-second timeout. Simpler, but saving a class of 40 could wait on 40 Firebase calls.
4. **Firebase project and packages:** a real push needs a Firebase project, which only the team can create (free; your Google account):
   - Android: `google-services.json` in `android/KlaslyApp/app/`, git-ignored.
   - API: a service-account JSON kept outside the repo, its path in user-secrets.

   **Recommended packages** (exact latest stable versions confirmed in `/plan`):
   - API: **`Google.Apis.Auth`** (Google's library for the service-account OAuth token), used by a typed `HttpClient` as guide §13 says. The alternative, `FirebaseAdmin`, hides the HTTP call and pulls in more.
   - Android: the **Firebase BoM + `firebase-messaging`** and the **`com.google.gms.google-services`** Gradle plugin.

   Until the project exists, everything except smoke step 8 can be built and tested. Pushes are logged as "FCM is not configured". **Do you already have a Firebase project, or should the plan include the setup steps for you to follow?**
5. **Test project:**
   - **Recommended:** rename `tests/Klasly.Worker.Tests` to `tests/Klasly.Tests` and reference the API project too. One LocalDB fixture, one `dotnet test`.
   - *Alternative:* a second project `tests/Klasly.API.Tests` with its own copy of the fixture.
