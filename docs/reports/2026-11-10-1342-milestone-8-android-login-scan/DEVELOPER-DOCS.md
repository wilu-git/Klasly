# Milestone 8 Tasks 1–8: Android login + Scan QR screen

## Summary
The team asked to build every remaining Milestone 8 task in one go. The Android app (`android/KlaslyApp`) now:
- logs students and parents in through `POST /api/auth/login`;
- registers the student's phone (Android ID) with `POST /api/students/me/device`;
- refuses teachers and admins;
- gives students a Scan QR screen (CameraX + ML Kit) that sends the QR text to `POST /api/attendance/scan` and shows **Present** / **Late** or the API's reason.

Everything except a live QR scan was checked on the `Pixel_9` emulator. **The real-phone scan checks (smoke steps 5–9) are still open**: the emulator camera can't see a rotating QR.

## Projects touched
ANDROID KOTLIN only. WEB API, WEB MVC, EF CORE, MSSQL and CONSOLE WORKER are unchanged (`git diff 244deab -- src` is empty).

## Files changed (all under `android/KlaslyApp/` unless noted)

| Path | Change | Why |
|---|---|---|
| `gradle/libs.versions.toml`, `build.gradle.kts`, `app/build.gradle.kts` | modified | Libraries, Kotlin serialization plugin, `BuildConfig.API_BASE_URL` from `local.properties` |
| `app/src/main/AndroidManifest.xml` | modified | INTERNET + CAMERA permissions, optional camera feature, `KlaslyApplication` |
| `app/src/debug/AndroidManifest.xml` | added | Cleartext HTTP for the local API, debug builds only |
| `app/src/main/res/values/strings.xml` | modified | App name "Klasly" |
| `app/src/main/res/values-night/themes.xml` | added | Dark window behind the splash in dark mode |
| `…/data/ApiText.kt` | added | `errorMessage` (problem-details → text) and `parseUtc` |
| `app/src/test/…/data/ApiTextTest.kt` | added | 5 JVM tests for the two functions above, written first |
| `…/data/Dtos.kt` | added | `@Serializable` request/response bodies of the three endpoints |
| `…/data/KlaslyApi.kt` | added | Retrofit interface: login, registerDevice, scan |
| `…/data/TokenStore.kt` | added | Session in EncryptedSharedPreferences (deprecated library, `shortcut:` comment) |
| `…/data/KlaslyRepository.kt` | added | `ApiResult`, the API's message on failure, 401 on a token call → log out |
| `…/AppContainer.kt` | added | `KlaslyApplication` + hand-built OkHttp/Retrofit/repository, session `StateFlow` |
| `…/ui/login/LoginViewModel.kt`, `LoginScreen.kt` | added | Login form; student login → register phone → save session; staff refused |
| `…/ui/parent/ParentHomeScreen.kt` | added | Placeholder until Milestone 13 |
| `…/ui/scan/ScanViewModel.kt`, `ScanScreen.kt` | added | Permission flow, camera + ML Kit, Scanning/Sending/Checked/Refused states |
| `…/MainActivity.kt` | modified | Picks Login / Scan / Parent home from `session.role` |
| `SPEC.md`, `tasks/plan.md`, `tasks/todo.md` (repo root) | modified | Ticks and deviations recorded |

## Library versions (checked against Maven on 2026-11-10)

| Library | Version | Note |
|---|---|---|
| AGP / Gradle / Kotlin / Compose BOM | 9.3.2 / 9.5.0 / 2.2.10 / 2026.02.01 | from the wizard files (Task 0) |
| Retrofit + converter-kotlinx-serialization | 3.0.0 | brings OkHttp 4.12 |
| kotlinx-serialization-json | 1.9.0 | 1.10+ needs Kotlin 2.3 |
| lifecycle-viewmodel-compose | 2.11.0 | same as the wizard's lifecycle-runtime |
| CameraX (camera2, lifecycle, view, mlkit-vision) | 1.6.2 | newest stable (1.7.0 is RC) |
| ML Kit barcode-scanning (bundled) | 17.3.0 | works offline, no Play services download |
| androidx.security:security-crypto | 1.1.0 | deprecated by Google, approved in the spec (Q2) |

## Key code

```kotlin
// LoginViewModel: the phone is registered BEFORE the session is saved, so a 409 leaves nothing behind.
when (login.role) {
    "Student" -> when (val device = container.repository.registerDevice(login.token)) {
        is ApiResult.Ok -> Unit
        is ApiResult.Failed -> return device.message   // "Another device is already registered…"
    }
    "Parent" -> Unit
    else -> return STAFF_REFUSED
}
container.logIn(Session(login.token, parseUtc(login.expiresAtUtc), login.role, login.fullName))
```

```kotlin
// KlaslyRepository: one place turns every failure into the API's own text.
} catch (e: HttpException) {
    val message = errorMessage(e.response()?.errorBody()?.string())
    if (e.code() == 401 && endsSessionOn401) onSessionExpired(message)   // not for login's 401
    ApiResult.Failed(message, e.code() == 401)
} catch (e: IOException) { ApiResult.Failed(NETWORK_ERROR) }
```

```kotlin
// ScanViewModel: one QR = one request; the camera keeps reading, but only Scanning accepts a read.
fun onQrRead(text: String) {
    if (_state.value != ScanUiState.Scanning) return
    _state.value = ScanUiState.Sending
    ...
}
```

Deviations from the plan, all small:
- `AppContainer` is created by a `KlaslyApplication` class, not by `MainActivity`. A rotation would otherwise build a second container whose session differs from the ViewModels'.
- The camera wiring lives in `ScanScreen.kt`; `QrAnalyzer.kt` wasn't needed.
- Tasks 4–5 and 6–7 were committed together, since each pair edits the same files.

## Database changes
None.

## How it was verified
- `./gradlew assembleDebug testDebugUnitTest` → BUILD SUCCESSFUL; **ApiTextTest 5/5** (it failed first, with 8 unresolved references, before `ApiText.kt` existed).
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors. No change under `src/`.
- Merged manifests: debug has `usesCleartextTraffic="true"`, release doesn't. `BuildConfig.API_BASE_URL` defaults to `http://10.0.2.2:5201/api/` and follows `klasly.apiBaseUrl` when that is set.
- **On the `Pixel_9` emulator** (API `http` profile, LocalDB, the seed students' devices reset first through `DELETE /api/accounts/{id}/device`):

| Smoke step | Result |
|---|---|
| 1 Login errors | Empty/invalid email → "The Email field is not a valid e-mail address."; wrong password → "Invalid email or password."; API stopped → "Can't reach Klasly. Check your connection." ✔ |
| 2 Roles | Teacher → "This app is for students and parents. Please use the Klasly website." ✔; Parent → "Hi, Marites Mendoza." placeholder → Log out → Login ✔ |
| 3 Student login | Aira → `Users.RegisteredDeviceId = 5c58161fd8060037` → scan screen; kill + reopen → still logged in ✔ |
| 4 Other phone | Bryan with `other-phone` already registered (through the API) → the 409 message, nothing saved ✔ |
| 5 Permission | First open asks; "Don't allow" → explanation + Allow camera → asks again → live preview ✔ |
| 6–8 Scan results | **Not checked yet: needs a real phone** |
| 9 Expiry | API with `Jwt__ExpiryMinutes=1` (env var, no file change): reopening the app after expiry → Login ✔. The 401-on-scan half needs the phone |
| 10 Rotation / TalkBack | Rotating to landscape keeps the typed email and the error ✔. Result/error texts are polite live regions (TalkBack itself not run) |
| 11 `/impeccable` | Polish pass done (see below) ✔ |

- `/impeccable` polish findings, all fixed:
  - white splash in dark mode → `values-night` theme;
  - the login subtitle was wrong for parents → "Log in with your school email.";
  - no autofill hints → email/password content types;
  - refusals lacked a state heading → "Not checked in" above the API's reason.
- Also checked in dark mode at 130 % font scale and in landscape: nothing clips.

## Known limits / TODO
- **Open:** smoke steps 6–9 on a real phone over USB (`adb reverse tcp:5201 tcp:5201` + `klasly.apiBaseUrl=http://localhost:5201/api/`), including the vertical slice: teacher opens a session → phone scans → the roster shows Present.
- The emulator's virtual-scene camera can load a QR poster (`-virtualscene-poster`), but the poster sits outside the default camera view and can't change every 30 s, so it can't stand in for the phone.
- The launcher icon is still the default Android robot (no Klasly icon asset yet).
- The API's first database call after starting sometimes times out while LocalDB wakes up (SQL error -2). This existed before Milestone 8; retrying works.
- The Android ID depends on the signing key: the same phone with debug builds from two different PCs counts as two devices (the admin resets it).
- The app has no Impeccable `PRODUCT.md` / `DESIGN.md` yet (`/impeccable init` can create them).
