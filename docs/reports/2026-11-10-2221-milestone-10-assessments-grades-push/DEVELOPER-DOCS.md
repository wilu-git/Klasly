# Milestone 10 — Assessments and grades (API + MVC) + FCM grade-posted push

## Summary

Built Milestone 10 Tasks 0–8 and 10 from `tasks/todo-m10.md`:
- **API:** teachers create weighted assessments (the class total never goes above 100%) and save a whole class's scores in one call. Rules are enforced in the API: 0–MaxScore, 2 decimals, active students only, all or nothing.
- **Push queue:** a student's first grade on an assessment writes a Pending push row in the same save. A background service in the API sends it through FCM HTTP v1, with the Google OAuth token built in-house (no NuGet package).
- **MVC:** the class page gets an Assessments section, and each assessment gets a grade sheet page.
- **Android:** the app registers its FCM token and shows the notification. Firebase is only switched on when `google-services.json` exists.

**Task 9 (Firebase project + real push on a device) waits on the team.** Until then every push is logged `Failed` / "FCM is not configured", and grades are unaffected.

## Projects touched

WEB API, EF CORE (repositories), WEB MVC, ANDROID KOTLIN, tests. MSSQL: no schema change.

## Files changed

| Path | Change | Why |
|---|---|---|
| `tests/Klasly.Worker.Tests` → `tests/Klasly.Tests` | renamed | One test project for Worker + API (spec Q5); + `Klasly.API` reference |
| `tests/Klasly.Tests/TestData.cs` | added | Shared "new class with seats" helper (used by old and new tests) |
| `tests/Klasly.Tests/{AssessmentServiceTests,GradeTests,PushTests,FcmTokenTests}.cs` | added | 22 new tests (33 total) |
| `src/Klasly.Core/DTOs/{AssessmentRequest,AssessmentResponse,GradeDtos,FcmTokenRequest}.cs` | added | API contracts |
| `src/Klasly.Core/Interfaces/{IAssessmentRepository,IGradeRepository,INotificationRepository}.cs` | added | Data access contracts |
| `src/Klasly.Core/Interfaces/IUserRepository.cs` | modified | + `SetFcmTokenAsync` |
| `src/Klasly.Infrastructure/Repositories/{Assessment,Grade,Notification}Repository.cs` | added | EF Core implementations |
| `src/Klasly.Infrastructure/Repositories/UserRepository.cs`, `DependencyInjection.cs` | modified | Token move (one transaction); registrations |
| `src/Klasly.API/Services/AssessmentService.cs` (+ interface) | added | Assessment and grade rules, push rows, enqueue after commit |
| `src/Klasly.API/Services/FieldValidationException.cs` | added | 400 with an `errors` object (`weightPercent`, `grades[2].score`) |
| `src/Klasly.API/Middleware/ApiExceptionHandler.cs` | modified | Writes `ValidationProblemDetails` for it |
| `src/Klasly.API/Push/{FcmCredentials,FcmPushClient,PushQueue,PushNotificationSender,PushSender}.cs` | added | In-house OAuth JWT, FCM v1 typed client, queue, sender, background service |
| `src/Klasly.API/Controllers/AssessmentsController.cs` | added | 6 endpoints |
| `src/Klasly.API/Controllers/StudentsController.cs`, `Services/AccountService.cs` (+ interface), `Program.cs` | modified | `PUT api/students/me/fcm-token`; DI for push |
| `src/Klasly.Web/Models/Api/AssessmentDtos.cs`, `Repositories/AssessmentRepository.cs` (+ interface), `Services/AssessmentService.cs` (+ interface) | added | API-backed MVC data layer |
| `src/Klasly.Web/ViewModels/AssessmentViewModels.cs` | added | Form, grade row, page models |
| `src/Klasly.Web/ViewModels/TeacherClassViewModel.cs`, `Services/SessionService.cs`, `Program.cs` | modified | Class page loads assessments; registrations |
| `src/Klasly.Web/Areas/Teacher/Controllers/AssessmentsController.cs` | added | Create, Details, Edit, SaveGrades (anti-forgery) |
| `src/Klasly.Web/Areas/Teacher/Views/Assessments/Details.cshtml`, `Views/Shared/_AssessmentForm.cshtml` | added | Grade sheet page; shared add/edit form |
| `src/Klasly.Web/Areas/Teacher/Views/Classes/Details.cshtml`, `wwwroot/css/site.css` | modified | Assessments section (above Sessions); score input width |
| `android/KlaslyApp/gradle/libs.versions.toml`, `build.gradle.kts`, `app/build.gradle.kts` | modified | Firebase BoM 35.1.0 + messaging; google-services 4.5.0 applied only if the JSON exists |
| `android/.../data/{KlaslyApi,KlaslyRepository,Dtos}.kt`, `AppContainer.kt`, `MainActivity.kt`, `AndroidManifest.xml` | modified | Token call, send on student login, permission ask, service + channel meta-data |
| `android/.../push/{PushRegistration,KlaslyMessagingService}.kt`, `res/drawable/ic_notification.xml` | added | Registration, receiver, notification icon |
| `.gitignore` | modified | `google-services.json` and `*-firebase-adminsdk-*.json` |
| `SPEC.md`, `tasks/plan-m10.md`, `tasks/todo-m10.md` | modified/added | Approved spec, plan, ticked tasks |

## Key code

**Grade save: validate every row, then one transaction, then enqueue** (`Klasly.API/Services/AssessmentService.cs`):

```csharp
if (errors.Count > 0) throw new FieldValidationException(errors);          // 400, nothing saved
...
grades.Add(new Grade { AssessmentId = assessmentId, StudentId = row.StudentId, Score = score, PostedAt = now });
notifications.Add(push);   // Push / GradePosted / Pending, DedupKey grade:{assessmentId}:{studentId}
...
if (added + changed > 0 && !await grades.TrySaveAsync(ct)) throw new ConflictException("Grades were just updated. Reload and try again.");
foreach (var push in pushes) pushQueue.Enqueue(push.NotificationId);       // sent in the background, after the commit
```

**Google OAuth without a package** (`Klasly.API/Push/FcmCredentials.cs`):
1. Sign an RS256 JWT (`iss` = client_email, `scope` = firebase.messaging, `aud` = token_uri, 1 h) with `RSA.ImportFromPem(private_key)`.
2. POST it as `grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer`.
3. Cache the access token until 5 minutes before it expires.

**Push outcome** (`PushNotificationSender`):

| Situation | Result |
|---|---|
| Not configured | `Failed` "FCM is not configured" |
| No token | `Failed` "No device registered for push" |
| FCM 200 | `Accepted` + `ExternalMessageId` (FCM's message name) |
| Otherwise | `Failed` + `LastError` |

There are no retries (`shortcut:` comment).

**Android without Firebase:** `if (file("google-services.json").exists()) apply(plugin = "com.google.gms.google-services")`, and every runtime call checks `FirebaseApp.getApps(context).isNotEmpty()`.

**Plan deviations (small):**
- Grade methods live in `AssessmentService` (no separate `GradeService`), to reuse the ownership helpers.
- The exception class is `FieldValidationException` (the plan called it `RowValidationException`), because it's used for form fields as well as rows.

## Database changes

None. `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration."

## How it was verified

- `dotnet build Klasly.slnx`: 0 warnings. `dotnet test --solution Klasly.slnx`: **33 passed, 0 failed** (11 before + 22 new).
  - Weights: exactly 100 OK, +0.01 refused; edits exclude themselves; 3-decimal values refused; max below a posted score refused; duplicate title → 409; another teacher → 403; graded counts ignore dropped students.
  - Grades: 0 and max OK; −0.01, max + 0.01 and 3 decimals refused with one error per row, and nothing saved; dropped student refused; blanks skipped; first grade → one Pending push, change → none, same → nothing; `DatePosted` set once.
  - Push: Accepted / FCM error / not configured / no token / not sent twice / startup pickup; the JWT's header, claims and signature checked with a generated RSA key.
  - Token: saved; moved from Bryan to Carla.
- `./gradlew assembleDebug testDebugUnitTest` **without** `google-services.json`: BUILD SUCCESSFUL. No warnings from the new code; the two deprecated FCM APIs are explicitly marked, see Known limits.
- **API smoke** (curl, real `Klasly` DB, class 1):
  - Quiz 1 (20, 30%) and Midterm (100, 70%) → 201; a third at 10% → 400 "can't go above 100%. 0% is left.";
  - 4 grades → "4 new";
  - a score of 21 → 400 `grades[1].score`;
  - a changed score → "0 new, 1 changed", `UpdatedAt` set, no second push row;
  - max 15 below a posted 20 → 400;
  - Mark → 403 ×2;
  - token: empty → 400, as a teacher → 403;
  - the `Notifications` rows all `Failed` "FCM is not configured", grades saved.
- **Browser** (preview pane):
  - weight error next to the Weight field, with the typed values kept;
  - grade sheet: the browser stops 21 itself (input max); posted anyway, the API's "Score must be between 0 and 20." lands on Ellen's row with every typed value kept;
  - valid save → "Grades saved: 2 new, 0 changed.";
  - an edit below a posted score → the error, with the Edit section opened automatically.
- **`/impeccable` (polish):**
  1. the Assessments section moved above the ever-growing session list;
  2. the Type column is hidden below 576 px (the table overflowed by 1 px);
  3. score inputs show "18" instead of "18.00".
  
  Both pages fit at 375 px with no sideways scroll. The detector on the changed views + CSS returned `[]`.
- `dotnet list package --include-transitive`: no Microsoft package below 10.x and **no new NuGet package**. `git status`: no Firebase files.

## Known limits / TODO

- **Task 9 (team):** create the Firebase project, add `google-services.json` and the service-account key (steps in HOW-TO-USE.md), then smoke steps 8–9 on a Google Play emulator or a phone. Checkpoint C stays open until then.
- **Deprecated FCM APIs:** `FirebaseMessaging.getToken()` / `onNewToken` were deprecated in firebase-messaging 25.1.0 (June 2026) in favour of installation-ID registration (`register()` + `onRegistered`). They still work in 26.0.1 (BoM 35.1.0). They're kept, with `shortcut:` comments, until sending to installation IDs can be verified on a real Firebase project.
- No push retries, and a stale (UNREGISTERED) token isn't cleared (`shortcut:` in `PushNotificationSender`).
- A duplicate title on a class that is already at 100% reports the weight error first; the title conflict shows after that's fixed.
- No assessment or grade delete (out of scope; the table has no soft-delete column).
- The smoke test left Quiz 1 and Midterm with grades and 4 Failed push rows in the dev `Klasly` database.
- `.claude/` is still untracked. Nothing is committed yet.
