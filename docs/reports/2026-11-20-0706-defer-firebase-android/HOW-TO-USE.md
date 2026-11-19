# How to use — Firebase deferred in the Android app

## Prerequisites
- Android Studio (its bundled Java is at `D:\Android Studio\jbr` on the dev machine).
- No Firebase project or `google-services.json` is needed any more.

## Step-by-step
1. In `android/KlaslyApp`, run `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew installDebug` (or press Run in Android Studio).
2. Log in as a student or parent and use the app as usual: Scan QR, My attendance, My grades, Child overview.
3. When a teacher posts a grade, no notification appears on the phone. The student sees the grade the next time they open My grades.

## Example
A student logs in. The app no longer asks "Allow Klasly to send you notifications?", because push is switched off.
Everything else on screen looks the same as before.

## Turning push back on later
1. Create a Firebase project and add an Android app with package `com.klasly.app`.
2. Download `google-services.json` into `android/KlaslyApp/app/` (never commit it).
3. Search the Android project for `DEFERRED` and uncomment each of those lines. In `KlaslyMessagingService.kt`, remove the `/*` and `*/` lines.
4. Set the API's service-account path: `dotnet user-secrets set "Fcm:ServiceAccountPath" "<path>" --project src/Klasly.API`.

## Troubleshooting
- **`JAVA_HOME is not set`** when running `gradlew`: run `$env:JAVA_HOME = "D:\Android Studio\jbr"` first.
- **Gradle sync fails with `Unresolved reference: firebase`** after re-enabling push: you uncommented the dependencies but not the
  catalog entries in `gradle/libs.versions.toml` (or the other way round). Uncomment both.
- **Push rows show Failed in the database**: expected while push is deferred. The reason column says why.
