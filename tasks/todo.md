# Milestone 8 — Tasks (Android login + Scan QR screen)

> Plan: [`plan.md`](./plan.md) · Spec: [`SPEC.md`](../SPEC.md). Android only: no API, Web or database change.
> Build after every task (PowerShell, from `android/KlaslyApp`): `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew assembleDebug testDebugUnitTest`.
> API for the app: `dotnet run --project src/Klasly.API --launch-profile http` (`http://localhost:5201`). Website for the teacher's QR: `dotnet run --project src/Klasly.Web --launch-profile https` (`https://localhost:7144`).
> Seed logins: teachers `rosa.villanueva@klasly.test` (ELECT3) and `mark.delossantos@klasly.test`, plus the seed admin, parents and students. Reset a student's phone in Swagger: `DELETE /api/accounts/{id}/device`.
> Every task also has to meet the **Definition of Done** in `plan.md`.

---

## Task 0 (team): create the project with the Android Studio wizard

**Description:** In Android Studio: **File → New → New Project → Empty Activity**, with:
- Name: `KlaslyApp`
- Package name: `com.klasly.app`
- Save location: `C:\Users\User\source\repos\wilu-git\Klasly\android\KlaslyApp`
- Minimum SDK: **API 26 ("Oreo")**
- Build configuration language: **Kotlin DSL (build.gradle.kts)** (the default)

Click **Finish** and wait for the first Gradle sync to end. Nothing else.

**Done differently (team decision, 2026-11-10):** instead of running the wizard, Claude copied the wizard-generated files from a recent project made by the same Android Studio (`ComposeNavigationSeatwork`: Gradle 9.5.0, AGP 9.3.2, Kotlin 2.2.10, Compose BOM 2026.02.01). Copied: the wrapper, Gradle files, launcher icons, `res/values`, `res/xml` and the `ui/theme` files. Renamed to `com.klasly.app` / `KlaslyTheme`, min SDK set to 26, navigation-compose removed, a fresh Empty Activity `MainActivity`. None of the seatwork's own screens were copied. Added `.gitattributes` so `gradlew` keeps LF line endings.

**Acceptance criteria:**
- [x] `android/KlaslyApp/gradlew.bat`, `gradle/libs.versions.toml` and `app/src/main/java/com/klasly/app/MainActivity.kt` exist.
- [x] `android/KlaslyApp/.gitignore` exists (it keeps `local.properties` and `build/` out of git; checked with `git check-ignore`).

**Verification:**
- [x] Build succeeds: `./gradlew assembleDebug` (BUILD SUCCESSFUL, first run 7 m 40 s for the downloads)

**Dependencies:** None

**Estimated scope:** Done by the team in about 2 minutes

---

## Task 1: Build setup

**Description:** Add the libraries from plan decision 9 to the version catalog and `app/build.gradle.kts`, plus the Kotlin serialization plugin. Turn on `buildConfig` and add `API_BASE_URL`, read from `local.properties` `klasly.apiBaseUrl` with the default `http://10.0.2.2:5201/api/`. Add the `INTERNET` and `CAMERA` permissions (camera hardware `required="false"`) to the main manifest, and `usesCleartextTraffic="true"` in a new `src/debug/AndroidManifest.xml`.

**Acceptance criteria:**
- [x] `./gradlew assembleDebug` passes with every library resolved.
- [x] The merged **debug** manifest has `usesCleartextTraffic="true"`, and the merged **release** manifest doesn't.
- [x] Without `klasly.apiBaseUrl`, `BuildConfig.API_BASE_URL` is `http://10.0.2.2:5201/api/`. With it set, it uses that value.

**Verification:**
- [x] Build succeeds: `./gradlew assembleDebug`
- [x] Manual check: the generated `BuildConfig.java`, and the merged manifests under `app/build/intermediates/merged_manifests/` after `./gradlew processDebugMainManifest processReleaseMainManifest`

**Dependencies:** Task 0

**Files likely touched:**
- `gradle/libs.versions.toml`, `build.gradle.kts` (root: plugin alias), `app/build.gradle.kts` (modified)
- `app/src/main/AndroidManifest.xml` (modified)
- `app/src/debug/AndroidManifest.xml` (new)

**Estimated scope:** Medium (5 files, small edits)

---

## Task 2: API text rules, test first

**Description:** Write `ApiTextTest` first and watch it fail. Then write `data/ApiText.kt` with `errorMessage(body: String?)` and `parseUtc(text: String)` (plan decision 4) until it passes. The functions use only `kotlinx.serialization.json` and `java.time`, with no Android types.

**Acceptance criteria:**
- [x] `errorMessage`: `{"detail":"Invalid QR code"}` → "Invalid QR code"; `{"errors":{"Email":["The Email field is required."]}}` → that message; only `title` → the title; `""`, `null` or `<html>` → "Something went wrong. Please try again."
- [x] `parseUtc`: `"2026-11-10T01:03:00Z"` and `"2026-11-10T01:03:00.1234567Z"` (7 decimals, as .NET sends them) → the same `Instant` to the second; `"2026-11-10T01:03:00"` (no offset) → read as UTC.
- [x] The test fails before `ApiText.kt` exists (8 unresolved references) and passes after (5/5).

**Verification:**
- [x] Tests pass: `./gradlew testDebugUnitTest`
- [x] Build succeeds: `./gradlew assembleDebug`

**Dependencies:** Task 1

**Files likely touched:**
- `app/src/main/java/com/klasly/app/data/ApiText.kt` (new)
- `app/src/test/java/com/klasly/app/data/ApiTextTest.kt` (new)

**Estimated scope:** Small (2 files)

---

## Task 3: API client + session store

**Description:** `Dtos.kt`: `@Serializable` `LoginRequest`, `LoginResponse`, `RegisterDeviceRequest`, `DeviceResponse`, `ScanRequest`, `ScanResponse`. `KlaslyApi.kt`: the Retrofit interface (the device call takes an explicit `Authorization` header for the login flow, plan decision 5). `TokenStore.kt`: EncryptedSharedPreferences with a `shortcut:` comment for the deprecation, and recovery from a broken keystore. `KlaslyRepository.kt`: `ApiResult`, `login`, `scan`, and clearing the session on 401, with error text from `errorMessage`. `AppContainer.kt`: OkHttp with the Bearer interceptor, Retrofit with `Json { ignoreUnknownKeys = true }`, the Android ID, and the session `StateFlow`. Created once in `KlaslyApplication` (registered in the manifest), so a rotation never builds a second container; no screen uses it yet.

**Acceptance criteria:**
- [x] The app builds and still starts (the template screen shows, no crash creating `AppContainer`).
- [x] No OkHttp logging interceptor, and no `Log` call with the token or the QR text.
- [x] `TokenStore` returns no session when the saved `expiresAtUtc` has passed.

**Verification:**
- [x] Build succeeds: `./gradlew assembleDebug`; tests still pass
- [x] Manual check: `./gradlew installDebug` on `Pixel_9`, the app opens

**Dependencies:** Task 2

**Files likely touched:**
- `app/src/main/java/com/klasly/app/data/Dtos.kt`, `KlaslyApi.kt`, `TokenStore.kt`, `KlaslyRepository.kt` (new)
- `app/src/main/java/com/klasly/app/AppContainer.kt` (new)

**Estimated scope:** Medium (5 files; the DTOs are one-liners)

### Checkpoint A: after Tasks 0–3
- [x] `./gradlew assembleDebug` and `./gradlew testDebugUnitTest` pass
- [x] The app installs and starts on `Pixel_9`

---

## Task 4: Student login → phone registered → stays logged in

**Description:** `LoginViewModel` + `LoginScreen`: email and password (show/hide), a spinner on the button while waiting, keyboard "Done" submits, and the error text. A student: login → register the device with the new token → save the session (plan decision 5). `MainActivity` switches on the session: Student → a temporary "Scan comes next" placeholder with **Log out** (replaced in Task 6), none → Login. App name set to "Klasly".

**Acceptance criteria:**
- [x] Wrong password → "Invalid email or password."; empty fields → the API's validation message ("The Email field is not a valid e-mail address."). *API stopped → checked in Task 7, together with the scan's network error.*
- [x] A student logs in → the database `Users.RegisteredDeviceId` = the app's Android ID (`5c58161fd8060037` for Aira on `Pixel_9`) → placeholder. Kill and reopen the app → still logged in. **Log out** → Login.
- [x] Another phone already registered → the 409 message, still on Login, nothing saved (reopen the app → Login). *Checked with Bryan, whose "other phone" was registered through the API first, instead of booting `Pixel_7`: the API sees the same thing.*

**Verification:**
- [x] Build succeeds: `./gradlew assembleDebug`; tests still pass
- [x] Manual check: smoke steps 1, 3 and 4 (reset the student's device in Swagger first)

**Dependencies:** Task 3

**Files likely touched:**
- `app/src/main/java/com/klasly/app/ui/login/LoginViewModel.kt`, `LoginScreen.kt` (new)
- `app/src/main/java/com/klasly/app/MainActivity.kt`, `app/src/main/res/values/strings.xml` (modified)

**Estimated scope:** Medium (4 files)

---

## Task 5: Parent home, teacher/admin refused, Log out

**Description:** In `LoginViewModel`, a Teacher or Admin role → "This app is for students and parents. Please use the Klasly website.", nothing saved. A Parent is saved without the device step. `ParentHomeScreen`: "Hi, {name}. Your child overview comes in a later update." with **Log out**. `MainActivity` routes Parent → `ParentHomeScreen`.

**Acceptance criteria:**
- [x] Teacher login (`rosa.villanueva`) → the message, still on Login; nothing saved. Admin takes the same `else` branch.
- [x] Parent login → placeholder with the parent's name → **Log out** → Login.
- [x] A parent's login doesn't call `students/me/device`. *The API doesn't log each request, but that endpoint is StudentOnly: a call would have answered 403 and blocked the parent's login, which succeeded.*

**Verification:**
- [x] Build succeeds: `./gradlew assembleDebug`; tests still pass
- [x] Manual check: smoke step 2

**Dependencies:** Task 4

**Files likely touched:**
- `app/src/main/java/com/klasly/app/ui/parent/ParentHomeScreen.kt` (new)
- `app/src/main/java/com/klasly/app/ui/login/LoginViewModel.kt`, `MainActivity.kt` (modified)

**Estimated scope:** Small (3 files)

### Checkpoint B: after Tasks 4–5
- [x] Smoke steps 1–4 pass on the emulator (API-stopped case moved to Task 7)
- [x] Review with the team before continuing *(skipped: the team asked to build all tasks in one go)*

---

## Task 6: Scan happy path

**Description:** `ScanViewModel` with Scanning → Sending → Result. `ScanScreen`: top bar with the student's name and **Log out**; camera permission request (denied → explanation + **Allow camera**; blocked for good → open app settings); `PreviewView` in `AndroidView` bound to a `LifecycleCameraController` with `MlKitAnalyzer` (QR only, plan decision 6); a square guide; "Checking you in…" spinner; Result card (Present green / Late amber, "ELECT3 · BSIT-2A", Manila time, **Done**). Replaces the Task 4 placeholder. For now, a failed scan shows the message with a **Scan again** button. Task 7 finishes the error states.

**Acceptance criteria:**
- [x] Permission denied → explanation + **Allow camera**; granted → live preview. *(Pixel_9: dialog on first open, "Don't allow" → card, Allow camera → dialog again → "While using the app" → preview.)*
- [ ] On a real phone (`adb reverse`), scanning the teacher's QR → **Present** with the subject, section and Manila time, and the teacher's roster shows the student within 5 s. *(The vertical slice.)*
- [ ] One QR sends exactly one scan request (API log); **Done** goes back to the preview.

**Verification:**
- [ ] Build succeeds: `./gradlew assembleDebug`; tests still pass
- [ ] Manual check: smoke steps 5–6 on a real phone

**Dependencies:** Task 5

**Files likely touched:**
- `app/src/main/java/com/klasly/app/ui/scan/ScanViewModel.kt`, `ScanScreen.kt` (new)
- `app/src/main/java/com/klasly/app/MainActivity.kt` (modified)

**Estimated scope:** Medium (3 files; the main risk of the milestone)

---

## Task 7: Scan refusals + expired session

**Description:** The Error state: the API's message (or "Can't reach Klasly. Check your connection.") with **Scan again**. A 401 from the scan clears the session, and Login shows "Session expired – please log in". An app opened after the token expired goes straight to Login.

**Acceptance criteria:**
- [ ] The guide's text word for word: "Already checked in" (second scan), "Invalid QR code" (any non-Klasly QR), "QR expired – rescan" (a QR photo kept 2 min), "Session is closed" (after End session), "Not enrolled in this class"; and a student on a session started 30 min ago → **Late**.
- [ ] API stopped → "Can't reach Klasly…"; **Scan again** returns to the preview. *(Login with the API stopped shows "Can't reach Klasly. Check your connection." on Pixel_9 ✔; the scan half needs the phone.)*
- [ ] With API `Jwt:ExpiryMinutes` = 1 (set back afterwards): the next scan → Login with "Session expired – please log in"; reopening the app after expiry → Login.

**Verification:**
- [ ] Build succeeds: `./gradlew assembleDebug`; tests still pass
- [ ] Manual check: smoke steps 7–9

**Dependencies:** Task 6

**Files likely touched:**
- `app/src/main/java/com/klasly/app/ui/scan/ScanViewModel.kt`, `ScanScreen.kt` (modified)
- `app/src/main/java/com/klasly/app/ui/login/LoginScreen.kt` (modified: shows `loginMessage`)

**Estimated scope:** Small (3 files)

### Checkpoint C: after Tasks 6–7
- [ ] Teacher opens a session → student scans on a real phone → the roster shows Present within 5 s
- [ ] Smoke steps 5–9 pass
- [ ] Review with the team before continuing

---

## Task 8: Accessibility, `/impeccable`, full smoke test, reports

**Description:** Add `liveRegion = Polite` to the result and error text, check 48 dp touch targets and content descriptions, and check that rotation keeps the state. Run `/impeccable` on the three screens and fix what it finds. Run all 11 smoke steps. Write the three reports and the INDEX line, with the library versions.

**Acceptance criteria:**
- [ ] TalkBack reads the result and error messages; rotating mid-result keeps the result.
- [ ] `/impeccable` findings fixed, or listed in the report with the reason.
- [ ] All 11 smoke steps pass; `dotnet build Klasly.slnx` still passes.

**Verification:**
- [ ] Build succeeds: `./gradlew assembleDebug testDebugUnitTest`; `dotnet build Klasly.slnx`
- [ ] Manual check: smoke steps 10–11, plus a quick re-run of steps 1–9
- [ ] Reports in `docs/reports/<yyyy-MM-dd-HHmm>-milestone-8-android-login-scan/`, one line added to `docs/reports/INDEX.md`

**Dependencies:** Task 7

**Files likely touched:**
- `ui/login/LoginScreen.kt`, `ui/scan/ScanScreen.kt`, `ui/parent/ParentHomeScreen.kt` (polish only)
- `docs/reports/<folder>/DEVELOPER-DOCS.md`, `HOW-TO-USE.md`, `FLOWCHARTS.md`, `docs/reports/INDEX.md`

**Estimated scope:** Medium

### Checkpoint D: complete
- [ ] Every success criterion in SPEC.md is met
- [ ] No API, Web or database change
- [ ] Ready for `/review`
