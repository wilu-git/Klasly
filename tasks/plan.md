# Implementation Plan: Milestone 8 — Android login + Scan QR screen

> **Plan approved 2026-11-10.** Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-10, open questions 1–3 as recommended). Task details, acceptance criteria and verification steps are in [`todo.md`](./todo.md).
> Milestone 7's plan is in git history (commit `244deab`).

## Overview

This milestone creates the Klasly Android app and gives it two jobs: log in, and scan the teacher's QR. A student logs in once (the phone gets registered), points the camera at the QR on the teacher's page, and sees **Present** or **Late**. That completes the vertical slice from Milestone 7. The work is **Android only**: no API, Web or database change.

## Facts from the code that shape the plan

- **The three endpoints exist** (Milestone 6): `POST api/auth/login` (anonymous), `POST api/students/me/device` (`StudentOnly` policy), `POST api/attendance/scan` (`[Authorize]`; the service answers a non-Student role with 401).
- **Errors are problem-details JSON.** `ApiExceptionHandler` puts the message in `detail`. Model validation (`[ApiController]`) puts it in `errors: { field: [messages] }`. The JWT challenge also writes problem-details for 401.
- **Enums travel as names** (`JsonStringEnumConverter(allowIntegerValues: false)`), so `status` is `"Present"` / `"Late"`. The app keeps it as a `String`.
- `ScanRequest` has **no validation attributes on purpose**, so an empty or junk QR text gets the guide's `400 "Invalid QR code"`, not a model-validation error.
- Device registration is **idempotent**: the same phone again → `200`; another phone → `409 "Another device is already registered. Ask the admin to reset it."` Reset: `DELETE api/accounts/{id}/device` (Admin).
- The API's `http` profile listens on `http://localhost:5201` only. `UseHttpsRedirection` doesn't redirect when the API runs without an HTTPS port.
- **Toolchain:** Android Studio 2026.1.2 with JDK 21 at `D:\Android Studio\jbr` (`java` is not on PATH), SDK at `D:\Android\sdk` (platforms 35–37.1), emulators `Pixel_9` and `Pixel_7` (API 36.1, Google APIs).

## Architecture decisions

1. **The session lives in one place.** `TokenStore` (EncryptedSharedPreferences) saves token, expiry, role and name. `AppContainer` exposes it as a `StateFlow<Session?>`, plus a one-off `loginMessage` ("Session expired – please log in"). `MainActivity` collects it and picks the screen with `when (session?.role)`. Logging out or a 401 just clears the store, and the screen switches by itself. No navigation library.
2. **One OkHttp interceptor adds `Authorization: Bearer …`** from `TokenStore` when a token exists. No logging interceptor (the token must never reach Logcat).
3. **The repository never throws to the UI.** It returns `ApiResult.Ok` or `ApiResult.Failed(message, unauthorized)`. A 401 on an authenticated call also clears the session.
4. **API text rules are pure functions in `data/ApiText.kt`, with no Android types**, so a plain JVM unit test covers them, written test first:
   - `errorMessage(body: String?)`: `detail` → the first message in `errors` → `title` → "Something went wrong. Please try again.";
   - `parseUtc(text)`: `Instant.parse`, and a time without an offset is read as UTC.
5. **Login is one ViewModel call that does two requests for students:** login, then register the device with the **new token passed explicitly** (not saved yet). The session is saved only after both succeed, so a 409 leaves nothing behind (spec assumption 5).
6. **Camera: CameraX `LifecycleCameraController` + `MlKitAnalyzer`** (the `androidx.camera:camera-mlkit-vision` artifact, part of CameraX) with an ML Kit barcode scanner limited to `FORMAT_QR_CODE`. This replaces a hand-written `ImageAnalysis.Analyzer`. The callback hands the first QR text to `ScanViewModel.onQrRead()`, which ignores reads unless the state is `Scanning`. That gives one request per QR, and **Done** / **Scan again** turn scanning back on.
7. **Times are shown in Manila** with `ZoneId.of("Asia/Manila")`, as "9:03 AM".
8. **API address:** `buildConfigField("String", "API_BASE_URL", …)` reads `klasly.apiBaseUrl` from `local.properties` (not committed). The default is `http://10.0.2.2:5201/api/`. A real phone uses `adb reverse tcp:5201 tcp:5201` and `http://localhost:5201/api/`.
9. **Library versions:** latest stable at build time, all in `gradle/libs.versions.toml`, added to what the wizard creates: Retrofit 3 + `converter-kotlinx-serialization`, OkHttp, `kotlinx-serialization-json` (+ the Kotlin serialization plugin), `lifecycle-viewmodel-compose`, CameraX (`camera-camera2`, `camera-lifecycle`, `camera-view`, `camera-mlkit-vision`), ML Kit `barcode-scanning` (bundled), `androidx.security:security-crypto` 1.1.0. Versions are listed in the report.

## Dependency graph

```
Task 0  (team) Android Studio wizard creates android/KlaslyApp
   │
   └── Task 1  Build setup (Gradle, permissions, debug cleartext, API_BASE_URL)
          │
          └── Task 2  API text rules, test first (ApiText.kt + ApiTextTest)
                 │
                 └── Task 3  API client + session store (DTOs, Retrofit, TokenStore, repository, AppContainer)
                        │
                        └── Task 4  Student login → phone registered → stays logged in
                               │
                               └── Task 5  Parent home, teacher/admin refused, Log out
                                      │
                                      └── Task 6  Scan happy path: camera → QR → Present/Late   ← main risk, the vertical slice
                                             │
                                             └── Task 7  Scan refusals + expired session
                                                    │
                                                    └── Task 8  Accessibility, /impeccable, full smoke test, reports
```

**Order and risk.** The camera (Task 6) is the main risk, but it needs a real student token, so it can't come before login. Tasks 4–5 are small, so the risk is reached early anyway. The happy path (Task 6) is built and checked on a real phone *before* the error states (Task 7), so a camera problem shows up before any polish is done.

**Parallel work:** none. Each task needs the one before it, and Tasks 4–7 all touch `MainActivity` and the same screens.

## Task list

### Phase 1: Foundation
- [x] Task 0: create the project (wizard files copied from a recent project on this PC, see `todo.md`)
- [x] Task 1: Build setup
- [x] Task 2: API text rules, test first
- [ ] Task 3: API client + session store

### Checkpoint A (after Tasks 0–3)
- [ ] `./gradlew assembleDebug` and `./gradlew testDebugUnitTest` pass
- [ ] The app installs and starts on `Pixel_9`

### Phase 2: Login
- [ ] Task 4: Student login → phone registered → stays logged in
- [ ] Task 5: Parent home, teacher/admin refused, Log out

### Checkpoint B (after Tasks 4–5)
- [ ] Smoke steps 1–4 pass on the emulators
- [ ] Review with the team before continuing

### Phase 3: Scan
- [ ] Task 6: Scan happy path
- [ ] Task 7: Scan refusals + expired session

### Checkpoint C (after Tasks 6–7)
- [ ] Teacher opens a session → student scans on a real phone → the teacher's roster shows Present within 5 s
- [ ] Smoke steps 5–9 pass
- [ ] Review with the team before continuing

### Phase 4: Finish
- [ ] Task 8: Accessibility, `/impeccable`, full smoke test, reports

### Checkpoint D (complete)
- [ ] All 11 smoke-test steps and every success criterion in SPEC.md are met
- [ ] `dotnet build Klasly.slnx` still passes; no API, Web or database change
- [ ] Reports written, `docs/reports/INDEX.md` updated, ready for `/review`

## Definition of Done (every task)

A task is checked off only when its acceptance criteria are met **and**:

- `./gradlew assembleDebug` passes and `./gradlew testDebugUnitTest` still passes (CLAUDE.md §4);
- the behaviour was seen running on the emulator or the phone, not only compiled;
- error paths from the acceptance criteria were tried, not just the happy path;
- no debug `Log` calls, commented-out code or `TODO`s left behind; a known limit has a `shortcut:` comment instead;
- no token, QR text, key or `local.properties` value is logged or committed;
- the changes stay inside the task's files (anything else is noted in the task first).

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| The emulator camera can't see the QR on the monitor | High (the slice can't be shown) | Approved answer to open question 3: a real phone over USB with `adb reverse`. Login and error cases are tested on the emulator. |
| Wizard Gradle/Kotlin versions clash with the newest library versions (for example, a library needs a newer `compileSdk` or AGP) | Medium | Keep the wizard's AGP and Kotlin versions. If a library needs more, use the newest library version that fits, and note it in the report. |
| Gradle from the command line can't find Java | Low | `$env:JAVA_HOME = "D:\Android Studio\jbr"` before `./gradlew` (in the spec's commands). |
| The seed students already have a device registered from Milestone 6/7 curl tests (`phone-1`, `phone-2`) | Medium (looks like a bug at first login) | Before Task 4's check, reset them in Swagger (`DELETE /api/accounts/{id}/device`). |
| The Android ID depends on the signing key, so debug builds from your two PCs give the same phone two different IDs | Medium | Known limit, written in HOW-TO-USE: if you swap PCs, the admin resets the device. Release signing (one key) removes it later. |
| EncryptedSharedPreferences throws on a rare device whose keystore is broken | Low | `TokenStore` catches the error, deletes the prefs file and treats it as logged out. The deprecation gets a `shortcut:` comment (approved answer to open question 2). |
| A second QR read arrives while the first request is still in flight | Low | `onQrRead` ignores reads unless the state is `Scanning` (decision 6). The API also answers a double scan with 409. |

## Open questions

None. The spec's three questions are approved. **Task 0 needs one of you:** run Android Studio's New Project → Empty Activity wizard with the settings in `todo.md` Task 0, then say it's done.
