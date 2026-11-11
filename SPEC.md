# Spec: Milestone 8 — Android login + Scan QR screen

> Status: **Approved** (2026-11-10): open questions 1–3 approved as recommended (wizard-created project, EncryptedSharedPreferences with a `shortcut:` comment, real phone over USB with `adb reverse`). **Change to Q1 (2026-11-10):** the team chose to have Claude copy the wizard-generated files from a recent Android Studio project on this PC instead of running the wizard (details in `tasks/todo.md` Task 0).
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.8 "QR attendance check-in") → `CLAUDE.md` (§3.6 Android Kotlin) → this spec.
> Milestone 7's spec (Web MVC Teacher session page) is in git history (commit `244deab`) and in `docs/reports/2026-11-09-2354-milestone-7-teacher-session-page/`.

## What exists today

- **No Android project yet.** Milestone 1 listed an "Android app shell", but `android/` was never created. This milestone creates it.
- **The API side is done.** Milestone 6 built everything the app calls: `POST /api/auth/login`, `POST /api/students/me/device` and `POST /api/attendance/scan` (all 7 checks). Milestone 7 checked that a real phone camera can read the teacher's QR.
- **Toolchain on this PC:** Android Studio 2026.1.2 (`D:\Android Studio`, its bundled JDK 21), SDK at `D:\Android\sdk` (platforms 35–37.1), emulators `Pixel_7` and `Pixel_9` (API 36.1, Google APIs). `java` is not on PATH, so command-line Gradle uses `JAVA_HOME = D:\Android Studio\jbr`.

## Assumptions (correct these before we build)

1. **Android only.** No API, Web MVC or database change. The app uses only the three endpoints above.
2. **Stack from CLAUDE.md §3.6:** Kotlin, Jetpack Compose, min SDK 26, MVVM (`ViewModel` + `StateFlow`), a repository class around Retrofit, **kotlinx.serialization** (not Gson) for JSON, **CameraX + ML Kit barcode scanning** (the bundled model, so it works offline and without a Play services download), joined by CameraX's own `camera-mlkit-vision` artifact.
3. **Kept small on purpose:**
   - no Hilt or other DI library: one small `AppContainer` object builds Retrofit, the repository and the token store;
   - no navigation library: three screens, picked by a `when` on the logged-in role in `MainActivity`;
   - no Room / local database.
4. **Who can use the app:**
   - **Student** → Scan QR screen.
   - **Parent** → a placeholder "Child overview comes in a later update" screen with **Log out** (the real screen is Milestone 13).
   - **Admin / Teacher** → refused at login: "This app is for students and parents. Please use the Klasly website." The token is not saved.
5. **Device binding (guide §27.8 check 6):**
   - the device ID is the phone's **Android ID** (`Settings.Secure.ANDROID_ID`), as the `ScanRequest` DTO already documents;
   - right after a Student logs in, the app calls `POST /api/students/me/device`;
   - `409` ("Another device is already registered. Ask the admin to reset it.") → the message shows on the Login screen and the token is **not** saved, because every scan from this phone would fail anyway.
6. **Session handling:**
   - the JWT and its `expiresAtUtc`, role and name are stored in EncryptedSharedPreferences (see open question 2);
   - on app start, a missing or expired token → Login screen;
   - any `401` from the API → token cleared → Login screen with "Session expired – please log in";
   - **Log out** clears the store. There is no logout endpoint (JWTs are stateless).
7. **The API decides everything.** The app does not check the QR text, the time or Present/Late. It sends what the camera read and shows the API's answer. Error text comes from the problem-details `detail` field (or the first validation error), so the messages match guide §27.8 word for word.
8. **API address:** `BuildConfig.API_BASE_URL`, default `http://10.0.2.2:5201/api/` (the emulator's name for the PC, CLAUDE.md §3.6). The API must run with the **`http` launch profile**. With the `https` profile, `UseHttpsRedirection` sends the emulator to `https://localhost:7256`, which the phone can't reach.
9. **Cleartext HTTP only in debug builds:** `android:usesCleartextTraffic="true"` lives in `src/debug/AndroidManifest.xml`, not in the main manifest.
10. **Times shown in Asia/Manila** (CLAUDE.md §3.1) with `java.time` (available from SDK 26). The API sends UTC.
11. **Package name `com.klasly.app`**, app name "Klasly", in `android/KlaslyApp/`. Not added to `Klasly.slnx`; Android Studio's own `.gitignore` keeps `build/`, `.gradle/` and `local.properties` out of git.
12. **Out of scope:**
    - FCM grade pushes (Milestone 10);
    - My attendance, My grades and Child overview (Milestone 13);
    - a release build, signing, or the Play Store;
    - an admin "Reset phone" button (Swagger: `DELETE /api/accounts/{id}/device`);
    - offline scan queueing (a scan needs the network, by design: the QR expires in 30–60 s);
    - dark/light theme options beyond what the Compose template gives.

## Objective

A student opens the Klasly app, logs in once, and points the camera at the teacher's QR. Within a second or two, the app says **"Present"** or **"Late"** with the class and the time, and the teacher's page (Milestone 7) shows the check-in within 5 s.

This completes the vertical slice: **teacher opens a session → student scans → teacher sees the check-in.**

Functional requirement covered: guide §27.9 **#4**, the student side (scan the QR in the Android app; the Web API records Present or Late).

### User stories

- As a **student**, I log in with my email and password and stay logged in until my token expires.
- As a **student**, the first phone I log in on becomes my registered phone. On another phone I'm told to ask the admin to reset it.
- As a **student**, I tap nothing extra to scan: the Scan screen opens the camera, reads the QR and sends it once.
- As a **student**, I see my result clearly: **Present** (green) or **Late** (amber), the subject code and section, and the scan time in Manila time. Then I tap **Done**.
- As a **student**, when a scan is refused, I see the API's reason ("QR expired – rescan", "Already checked in", "Not enrolled in this class", …) and can tap **Scan again**.
- As a **student** whose token has expired, I'm sent back to Login with "Session expired – please log in".
- As a **parent**, I can log in and log out. My overview comes later.
- As a **teacher or admin**, the app tells me to use the website.

## Screens

| Screen | Shows | Calls |
|---|---|---|
| **Login** | Email, password (hidden, with show/hide), **Log in** button, error text. Loading spinner on the button while waiting. Keyboard "Done" submits. | `POST auth/login`, then (Student only) `POST students/me/device` |
| **Scan QR** (Student) | Greeting with the student's name, **Log out** in the top bar, camera preview filling the screen with a square guide. States below. | `POST attendance/scan` |
| **Parent home** (placeholder) | "Hi, {name}. Your child overview comes in a later update." and **Log out** | none |

### Scan QR states

| State | What the student sees | Next |
|---|---|---|
| **No camera permission** | "Klasly needs the camera to scan your teacher's QR code." + **Allow camera** button (asks again; if blocked for good, opens app settings) | Granted → Scanning |
| **Scanning** | Live preview, "Point your camera at the QR code on the screen" | First QR read → Sending (the analyzer stops, so one QR = one request) |
| **Sending** | Preview frozen behind a spinner, "Checking you in…" | 200 → Result; error → Error |
| **Result** | Big **Present** (green) or **Late** (amber) badge, "ELECT3 · BSIT-2A", "9:03 AM", **Done** button | Done → Scanning |
| **Error** | The API's message (or "Can't reach Klasly. Check your connection." when there's no response) + **Scan again** | Scan again → Scanning; a 401 goes to Login instead |

The result and error text sit in a live region (`liveRegion = Polite`) so TalkBack reads them. Buttons are at least 48 dp tall.

## API contract used (already built in Milestone 6)

| Call | Body | Success | Errors the app shows |
|---|---|---|---|
| `POST auth/login` | `{ "email", "password" }` | `200 { token, expiresAtUtc, userId, fullName, role }` | `400` validation, `401` "Invalid email or password." |
| `POST students/me/device` (Bearer) | `{ "deviceId" }` | `200 { deviceId }` | `409` "Another device is already registered. Ask the admin to reset it." |
| `POST attendance/scan` (Bearer) | `{ "qrToken", "deviceId" }` | `200 { status: "Present" \| "Late", scannedAtUtc, subjectCode, section }` | `401`, `400` "Invalid QR code" / "QR expired – rescan", `403` "Not enrolled in this class" / "Unregistered device", `409` "Session is closed" / "Already checked in" |

## Commands

```bash
# API for the emulator (http profile, see assumption 8)
dotnet run --project src/Klasly.API --launch-profile http          # http://localhost:5201

# Android build and unit test (PowerShell: $env:JAVA_HOME = "D:\Android Studio\jbr" first)
cd android/KlaslyApp
./gradlew assembleDebug
./gradlew testDebugUnitTest
./gradlew installDebug                                              # emulator or USB phone

# Real phone over USB (open question 3)
adb reverse tcp:5201 tcp:5201
```

## Project structure (files this milestone creates)

```
android/KlaslyApp/                         (new, from the Android Studio "Empty Activity" wizard: open question 1)
  settings.gradle.kts, build.gradle.kts, gradle/libs.versions.toml, gradlew(.bat), gradle/wrapper/
  app/build.gradle.kts                     (modified) libraries, BuildConfig.API_BASE_URL, kotlinx.serialization plugin
  app/src/main/AndroidManifest.xml         (modified) INTERNET + CAMERA permissions
  app/src/debug/AndroidManifest.xml        (new) usesCleartextTraffic = true, debug only
  app/src/main/java/com/klasly/app/
    MainActivity.kt                        (modified) picks Login / Scan / Parent home from the session state
    AppContainer.kt                        (new) builds OkHttp (Bearer header), Retrofit, repository, token store
    data/KlaslyApi.kt                      (new) Retrofit interface: login, registerDevice, scan
    data/Dtos.kt                           (new) @Serializable request/response classes
    data/ApiText.kt                        (new) errorMessage (problem-details → text) and parseUtc, no Android types
    data/KlaslyRepository.kt               (new) calls the API, turns errors into ApiResult with the API's message
    data/TokenStore.kt                     (new) EncryptedSharedPreferences: token, expiry, role, name
    ui/login/LoginViewModel.kt, LoginScreen.kt     (new)
    ui/scan/ScanViewModel.kt, ScanScreen.kt        (new) states above; CameraX LifecycleCameraController + MlKitAnalyzer
                                                   (camera-mlkit-vision) reports the first QR once (plan decision 6)
    ui/parent/ParentHomeScreen.kt          (new) placeholder + Log out
  app/src/test/java/com/klasly/app/data/ApiTextTest.kt        (new) error-text and UTC-parsing rules
```

## Code style

Kotlin official style, one screen = one `ViewModel` exposing a single `StateFlow<UiState>`, and screens stay stateless (state in, events out). The repository never throws to the UI: it returns a result with the message to show.

```kotlin
// data/KlaslyRepository.kt
sealed interface ApiResult<out T> {
    data class Ok<T>(val value: T) : ApiResult<T>
    data class Failed(val message: String, val unauthorized: Boolean = false) : ApiResult<Nothing>
}

suspend fun scan(qrToken: String): ApiResult<ScanResponse> = call {
    api.scan(ScanRequest(qrToken = qrToken, deviceId = deviceId))
}

private suspend fun <T> call(block: suspend () -> T): ApiResult<T> = try {
    ApiResult.Ok(block())
} catch (e: HttpException) {
    ApiResult.Failed(errorMessage(e.response()?.errorBody()?.string()), unauthorized = e.code() == 401)
} catch (e: IOException) {
    ApiResult.Failed("Can't reach Klasly. Check your connection.")
}
```

## Testing strategy

- **Build:** `./gradlew assembleDebug` → BUILD SUCCESSFUL. `dotnet build Klasly.slnx` still passes (nothing .NET changes).
- **One JVM unit test class** (`ApiTextTest`), written first: the error-text and UTC-parsing rules are the only non-trivial logic outside the camera. It checks that `detail` wins, then the first validation error, then a fallback for an empty or non-JSON body; and that .NET's 7-decimal UTC times and times without an offset parse as UTC.
- **Manual smoke test** with the API (`http` profile) and the website running on LocalDB, the seed accounts, and the teacher's session page from Milestone 7 open on the monitor. First, reset the test student's device in Swagger (`DELETE /api/accounts/{id}/device`): Milestone 6/7 testing registered `phone-1` / `phone-2` by curl.
  1. App start with no token → Login. Wrong password → "Invalid email or password." Empty fields → the API's validation message. API stopped → "Can't reach Klasly…".
  2. Teacher login → "This app is for students and parents…", stays on Login. Parent login → placeholder → Log out → Login.
  3. Student login → device registered (the user's `RegisteredDeviceId` in the database = the phone's Android ID) → Scan screen. Kill and reopen the app → still on Scan (token kept).
  4. Same student on a second device/emulator → the 409 message on Login. Reset in Swagger → login works on the second device.
  5. Camera permission denied → explanation + **Allow camera**; granted → preview.
  6. Scan the teacher's QR → **Present** with subject, section and Manila time; the teacher's roster shows the student within 5 s. *(The vertical slice.)*
  7. Scan again → "Already checked in". A session created with start = now − 30 min → a different student scans → **Late**.
  8. Any other QR (a URL) → "Invalid QR code". A QR photo kept for 2 min → "QR expired – rescan". End the session, then scan → "Session is closed". A student not in the class → "Not enrolled in this class".
  9. Token expiry (API `Jwt:ExpiryMinutes` = 1, as in Milestone 5) → the next scan goes to Login with "Session expired – please log in".
  10. TalkBack reads the result and error messages; rotate the phone mid-scan and the state is kept (ViewModel).
  11. `/impeccable` run on the three screens; findings fixed or listed in the report.

## Boundaries

- **Always:**
  - every rule decided by the API; the app only shows its answers;
  - MVVM: no API call from a composable;
  - loading, empty and error states on every screen (CLAUDE.md §3.6);
  - the token only in EncryptedSharedPreferences, never in logs (OkHttp logging off, or headers redacted, in debug);
  - `./gradlew assembleDebug` after every Android change; the three reports when built.
- **Ask first:**
  - any library not named in this spec (Hilt, navigation-compose, Room, Coil, …);
  - any API or database change (for example, to make the API reachable on the LAN);
  - Windows firewall or network changes (those are the team's to make);
  - a screen not in the table above.
- **Never:**
  - commit `local.properties`, keystores, `google-services.json` or any key;
  - allow cleartext HTTP in the release build;
  - decide Present / Late, or check the QR, on the phone;
  - log the JWT or the QR token text.

## Success criteria

- [ ] `android/KlaslyApp` builds with `./gradlew assembleDebug` and installs on the `Pixel_9` emulator and on a real phone.
- [ ] All 11 smoke-test steps behave as written.
- [ ] Step 6 works end to end: teacher opens a session on the website → student scans with the app → the teacher's roster shows Present within 5 s.
- [ ] Every refusal shows the guide's message word for word (§27.8 table).
- [ ] No API, Web or database change; the .NET solution still builds.
- [ ] The library list with versions is in the report.
- [ ] Reports written; `docs/reports/INDEX.md` updated.

## Open questions (my recommendation first)

1. **How do we create the Android project?**
   **Recommended:** one of you runs Android Studio's **New Project → Empty Activity** wizard (about 2 minutes): name `KlaslyApp`, package `com.klasly.app`, save location `…\Klasly\android\KlaslyApp`, minimum SDK **API 26**, build language **Kotlin DSL**. That gives a Gradle wrapper and plugin versions that are known to work with your Android Studio 2026.1.2. I then add everything else to it.
   *Alternative:* I write all the Gradle files by hand. That works, but the Gradle wrapper (`gradle-wrapper.jar`) can't be written as text, so it needs a download or a first sync in Android Studio anyway, and version mismatches are likely.
2. **EncryptedSharedPreferences is deprecated.** CLAUDE.md asks for it, but Google deprecated the whole `androidx.security:security-crypto` library in 2025 (version 1.1.0). It still works on SDK 26+; it just won't get fixes.
   **Recommended:** use it anyway, as CLAUDE.md says, with a `shortcut:` comment in `TokenStore`. It's one file, and swapping it later only touches that file.
   *Alternative:* DataStore + an AES key in the Android Keystore, written by hand (about 60 more lines of crypto code for a school project).
3. **How do we test a real scan?** The emulator's camera can't easily see a QR on your monitor.
   **Recommended:** a real Android phone on **USB** with `adb reverse tcp:5201 tcp:5201`. The phone then reaches the PC's API at `http://localhost:5201/api/`, with no LAN IP, no firewall change and no API change. The base URL becomes overridable from `local.properties` (`klasly.apiBaseUrl=http://localhost:5201/api/`, not committed). The default stays `10.0.2.2` for the emulator, as CLAUDE.md says. Login and all error cases are still tested on the emulator.
   *Alternatives:* the emulator with the laptop webcam as its back camera (`hw.camera.back = webcam0`) pointed at the screen (awkward); or the phone on Wi-Fi with the API on `0.0.0.0` and a firewall rule (a system change, and really Milestone 14's "host the phone can reach").
