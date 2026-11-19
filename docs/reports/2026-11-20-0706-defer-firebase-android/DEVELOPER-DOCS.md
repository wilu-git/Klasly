# Developer docs — Defer Firebase in the Android app

## Summary
The team can't set up a Firebase project yet, so they asked to remove Firebase from the Android app or comment it out as deferred.
Every Firebase line in the Android app is now commented out and tagged `DEFERRED`: the Gradle plugin, the dependencies, the
catalog entries, the manifest service, permission and meta-data, and the Kotlin code. `PushRegistration.isAvailable()` now always
returns `false`, so the app never tries to register for push. Everything else in the app works as before.
The Web API side is unchanged: it already marks push rows Failed with "FCM is not configured" when no service account is set.

## Projects touched
ANDROID KOTLIN only.

## Files changed
| Path | Change | Why |
|---|---|---|
| `android/KlaslyApp/build.gradle.kts` | modified | Commented out the `google-services` plugin alias |
| `android/KlaslyApp/app/build.gradle.kts` | modified | Commented out the conditional `google-services` apply and the `firebase-bom` / `firebase-messaging` dependencies; added the "how to turn it back on" note |
| `android/KlaslyApp/gradle/libs.versions.toml` | modified | Commented out the `firebaseBom`, `googleServices` versions and the Firebase library/plugin entries |
| `android/KlaslyApp/app/src/main/AndroidManifest.xml` | modified | Commented out `POST_NOTIFICATIONS`, the `KlaslyMessagingService` service and the two FCM meta-data entries |
| `android/KlaslyApp/app/src/main/java/com/klasly/app/push/PushRegistration.kt` | modified | Firebase imports commented out; `isAvailable()` returns `false`; the token fetch in `sendToken()` is commented out |
| `android/KlaslyApp/app/src/main/java/com/klasly/app/push/KlaslyMessagingService.kt` | modified | Whole class wrapped in a `/* ... */` block (Kotlin block comments nest, so the KDoc inside is safe) |

## Key code
```kotlin
/** DEFERRED: Firebase is switched off, so push is never available and every push call is skipped. */
fun isAvailable(@Suppress("UNUSED_PARAMETER") context: Context): Boolean = false
// fun isAvailable(context: Context): Boolean = FirebaseApp.getApps(context).isNotEmpty()
```
`AppContainer` still calls `PushRegistration.createChannel()` and `sendToken()`. Both are harmless: the channel is a plain
Android notification channel, and `sendToken()` returns straight away because `isAvailable()` is false.
`AskNotificationPermissionOnce()` is also guarded by `isAvailable()`, so students are never asked for notification permission.

## Database changes
None.

## How it was verified
- `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew assembleDebug testDebugUnitTest` → **BUILD SUCCESSFUL** (3 m 32 s). The only
  warnings are the two existing "Redundant creation of Json format" ones in `DisplayTextTest.kt`, which this change didn't touch.
- `graphify update .` run.

## Known limits / TODO
- No grade-posted push on the phone. Students see new grades only when they open My grades.
- The Web API still saves a push row for every posted grade and marks it Failed ("FCM is not configured" or "No device registered
  for push"). That is expected while push is deferred.
- To restore: uncomment every line tagged `DEFERRED` in the six files above, remove the `/*` and `*/` markers in
  `KlaslyMessagingService.kt`, put `google-services.json` in `android/KlaslyApp/app/`, and set the API secret `Fcm:ServiceAccountPath`.
