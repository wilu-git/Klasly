# Android → live API connection and QR check-in check

## Summary
The team set `klasly.apiBaseUrl=https://klasly.site/api` in `android/KlaslyApp/local.properties` and asked for a check that the app reaches the API, and that a QR scan records valid attendance.
The URL had no trailing `/`. Retrofit throws `IllegalArgumentException: baseUrl must end in /` when the app starts, so Gradle now adds the slash if it's missing.
The live API answered login and both read endpoints. The full QR flow (create → open → QR → register device → scan → roster) was run against the local API and recorded a valid **Present**.

## Projects touched
ANDROID KOTLIN (build config only). WEB API exercised, not changed.

## Files changed
| Path | Change | Why |
|---|---|---|
| `android/KlaslyApp/app/build.gradle.kts` | modified | Normalise `apiBaseUrl` to end in exactly one `/` |

## Key code
```kotlin
// Retrofit throws at startup unless the base URL ends in "/", so add it if local.properties left it off.
val apiBaseUrl: String = (localProperties.getProperty("klasly.apiBaseUrl") ?: "http://10.0.2.2:5201/api/").trimEnd('/') + "/"
```
The generated `BuildConfig.API_BASE_URL` is now `"https://klasly.site/api/"`.

## Database changes
None. The test left this local test data behind: session **2019** (ELECT3, closed), with one **Present** row for Francis Nuqui. His test device was reset afterwards (`DELETE /api/accounts/9/device` → 204). The live database was only read, never written.

## How it was verified
- `./gradlew assembleDebug testDebugUnitTest` (JAVA_HOME = Android Studio `jbr`): exit 0, APK 39.9 MB.
- Live `https://klasly.site/api`:
  - `POST auth/login` returned 200 for both the student (Dennis) and the parent (Rodel);
  - `GET students/me/overview` returned 200 and `GET parent/children` returned 200;
  - a wrong password returned 401.
- Local API `https://localhost:7256/api`:
  - the teacher created and opened session 2019 and got a 57-character QR token that refreshes every 30 s;
  - the student registered a device, then scanned and got **200 Present**;
  - a second scan returned 409 "Already checked in";
  - a tampered token returned 400 "Invalid QR code";
  - a teacher scanning returned 401;
  - the teacher roster shows Francis Present with source `Scan`;
  - the session closed.
- `dotnet test`: 163/163 earlier in the session (no .NET change since).

## Live scan (run 08:38 Manila, after the team approved it)
- Rosa created and opened live session **21** (ELECT3). Ellen registered a test device and scanned: **200 Present**. A repeat scan returned 409 and a tampered token returned 400.
- Session 21 was closed at 00:39:20 UTC, and the other five students were set to **Excused** (Manual) by 00:39:23. This was before the Worker's next one-minute tick, so the Worker marked no one Absent and queued no absence alerts. The Worker then set the session to Finalized.
- Ellen's device was reset (`DELETE /api/accounts/8/device` → 204).
- Before/after diff of every student's numbers: only Excused +1 (others) and Present +1 (Ellen) changed. Every absence rate, average and risk badge is the same. Dennis is still 2/12 = 0.1667, so one demo absence still flips him.
- Session 21 stays in the ELECT3 history (soft delete only).
- Found on live, not caused by this test: Marites and Linda now have non-seed emails, so they can't log in with `@klasly.test`. Aira also shows a second, empty ELECT3 class.

## Known limits / TODO
- A scan from the real phone's camera on klasly.site is still to do. The team should do one before the demo, then use Admin › Accounts › Reset phone if that phone shouldn't keep the student.
- The new APK hasn't been run on the emulator or a phone.
