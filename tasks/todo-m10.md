# Tasks: Milestone 10 — Assessments and grades + FCM grade-posted push

> From [`plan-m10.md`](./plan-m10.md) and [`SPEC.md`](../SPEC.md). Every .NET task ends with `dotnet build Klasly.slnx` + `dotnet test --solution Klasly.slnx`; Android tasks with `./gradlew assembleDebug testDebugUnitTest`.

## Task 0: Housekeeping
- [x] SPEC.md marked Approved with the answers; `plan-m10.md` + this file written; `google-services.json` and Firebase key files git-ignored.

## Task 1: Rename the test project
- [x] `tests/Klasly.Worker.Tests` → `tests/Klasly.Tests` (namespace, `Klasly.slnx`, + `Klasly.API` reference); the 11 tests still pass.

## Task 2: Assessments API (test first)
- [x] `GET/POST api/classes/{classId}/assessments`, `GET/PUT api/assessments/{id}` (Teacher, own class).
- [x] Tests: class total exactly 100 OK, 100.01 refused; edit excludes itself; a max score below a posted score refused; a duplicate title → 409; another teacher → 403.

## Task 3: Grades API (test first)
- [x] `GET/PUT api/assessments/{id}/grades`; row errors as `errors["grades[i].score"]`.
- [x] Tests: 0 and MaxScore OK; −0.01, MaxScore + 0.01, 3 decimals refused; one bad row saves nothing; a dropped student refused; a new grade → exactly one Pending push row; a changed score → no push row; the same score → nothing; `DatePosted` set once.
- [x] **Checkpoint A:** smoke steps 1–6 via curl.

## Task 4: Push sender (test first)
- [x] `FcmPushClient` (in-house RS256 JWT → Google token, cached; FCM HTTP v1 send), `PushQueue`, `PushSender` background service; grade save enqueues new notification ids.
- [x] Tests (fake client): Accepted + ExternalMessageId; error → Failed + LastError; not configured → Failed; no token → Failed; Pending rows picked up on start; the JWT test (header, claims, signature).

## Task 5: FCM token endpoint (test first)
- [x] `PUT api/students/me/fcm-token` (Student) → 204; the same token moves from another user; 400 on empty.
- [x] **Checkpoint B:** smoke step 7 (rows Failed "FCM is not configured"; grades unaffected).

## Task 6: MVC class page — Assessments section
- [x] Table (title, type, max, weight, graded "x of y"), "Weights: X of 100%" + warning under 100, **Add assessment** form with API errors on the form.

## Task 7: MVC assessment page
- [x] Edit form; a grade roster with score inputs and **Save grades**; row errors on their rows; "Grades saved: X new, Y changed."

## Task 8: Android push receiver
- [x] Firebase BoM 35.1.0 + messaging; the google-services plugin 4.5.0 applied only when `app/google-services.json` exists (the build passes without it).
- [x] The token is sent after a Student login and on `onNewToken`; a notification on push; `POST_NOTIFICATIONS` asked once (Android 13+); Firebase calls skipped when not configured.

## Task 9: Firebase setup (team) + real push
- [ ] Firebase project, `google-services.json`, service-account key outside the repo, `Fcm:ServiceAccountPath` user-secret.
- [ ] Smoke steps 8–9 on an emulator with Google Play or a phone.

## Task 10: Finish
- [x] `/impeccable` on the new pages; smoke steps 1–7 and 10 (8–9 wait for Task 9); no pending model changes; Microsoft.* 10.x and no new NuGet; no Firebase files tracked; reports + INDEX.

### Checkpoint C: Complete
- [ ] Every success criterion in SPEC.md met; ready for `/review`.
