# How to use: assessments, grade entry and grade notifications

## Prerequisites

- SQL Server LocalDB with the `Klasly` database.
- The Web API and the MVC website running:

  ```bash
  dotnet run --project src/Klasly.API --launch-profile https
  ```

  ```bash
  dotnet run --project src/Klasly.Web --launch-profile https
  ```

- Teacher login (fictional test data): `rosa.villanueva@klasly.test`, password `Klasly@123`.
- For phone notifications only: a Firebase project (see "Set up Firebase" below) and the Klasly app on an emulator with Google Play, or a phone.

## Step by step (teacher, website)

1. Log in and open **My classes → ELECT3 – BSCpE 3-A**.
2. Under **Assessments**, fill in **Add an assessment**:
   - title (e.g. *Quiz 1*);
   - type (pick or type: Quiz, Exam, Activity, Project, Recitation);
   - max score (e.g. 20);
   - weight (e.g. 30%).
   
   Press **Add**.
3. You land on the assessment's **grade sheet**. Type a score for each student (0 to the max, up to 2 decimals). Leave a box blank if that student isn't graded yet.
4. Press **Save grades**. The page says e.g. "Grades saved: 4 new, 0 changed."
   - A wrong score shows its message on that student's row, and nothing is saved until every row is fine. Your other entries stay in the boxes.
5. To change a score later, open the sheet again from the class page (**Grades** button), change it and save.
6. **Edit assessment** (at the bottom of the sheet) changes the title, type, max score or weight.
   - The class weights can't go above 100% in total.
   - The max score can't be lower than a score already saved.
7. The class page shows "Weights: X of 100%". You can post grades before it reaches 100%; a yellow note reminds you.

**Student side:** the first time a student's grade is saved for an assessment, their phone shows **"New grade posted – ELECT3 – Quiz 1: 18 / 20"**. This needs Firebase to be set up. A changed score doesn't send a second notification.

## Set up Firebase (once, team) — Milestone 10 Task 9

1. Go to <https://console.firebase.google.com>, choose **Add project**, name it e.g. `klasly-dev` (Google Analytics: off is fine).
2. In the project, choose **Add app → Android**. Package name: `com.klasly.app`. Download **`google-services.json`** and put it in `android/KlaslyApp/app/`. It is git-ignored; never commit it.
3. **Project settings → Service accounts → Generate new private key.** Save the JSON **outside the repo**, e.g. `C:\secrets\klasly-fcm.json`.
4. Tell the API where it is (user-secrets, not appsettings):

   ```bash
   dotnet user-secrets set "Fcm:ServiceAccountPath" "C:\secrets\klasly-fcm.json" --project src/Klasly.API
   ```

5. Rebuild the app (`./gradlew assembleDebug`) and install it on an emulator **with Google Play** (or a phone). Log in as a student and allow notifications when asked.
6. Post that student's first grade on the website. Within ~10 s the phone shows the notification, and the `Notifications` row says `Accepted`.

## Example (API)

```http
PUT /api/assessments/1/grades
Authorization: Bearer <teacher JWT>
Content-Type: application/json

{ "grades": [ { "studentId": 1, "score": 18 }, { "studentId": 6, "score": 21 } ] }
```

→ `400`:

```json
{ "errors": { "grades[1].score": ["Score must be between 0 and 20."] } }
```

Nothing is saved.

With valid scores → `200 { "added": 2, "changed": 0, "grades": [ … ] }`.

## Troubleshooting

| Problem | Fix |
|---|---|
| "The class weights can't go above 100%. N% is left." | Lower this assessment's weight, or edit another assessment's weight first. |
| No phone notification; the `Notifications` row says "FCM is not configured" | Do "Set up Firebase" steps 3–4 and restart the API. |
| Row says "No device registered for push" | The student hasn't logged in on a phone with Firebase set up since the app was rebuilt with `google-services.json`. Log in again. |
| The app built fine but never asks for notifications | `google-services.json` was missing at build time. Add it and rebuild. |
