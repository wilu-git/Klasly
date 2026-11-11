# Milestone 8 Task 0: Android project shell (KlaslyApp)

## Summary
Milestone 8 needs an Android app, but `android/` was never created (Milestone 1's "app shell" was skipped). The team chose to have Claude build the shell from the wizard-generated files of a recent Android Studio project on this PC, instead of running the New Project wizard by hand. The result is an Empty Activity Compose app in `android/KlaslyApp` (package `com.klasly.app`, min SDK 26) that builds with `./gradlew assembleDebug`. It has no Klasly features yet: login comes in Task 4 and scanning in Task 6.

## Projects touched
ANDROID KOTLIN only. No WEB API, WEB MVC, EF CORE, MSSQL or CONSOLE WORKER change.

## Files changed

| Path (under `android/KlaslyApp/`) | Change | Why |
|---|---|---|
| `gradlew`, `gradlew.bat`, `gradle/wrapper/*` | added | Gradle wrapper 9.5.0, so anyone can build without installing Gradle |
| `settings.gradle.kts`, `build.gradle.kts`, `gradle.properties`, `gradle/gradle-daemon-jvm.properties` | added | Wizard Gradle setup; project name `KlaslyApp` |
| `gradle/libs.versions.toml` | added | Version catalog (AGP 9.3.2, Kotlin 2.2.10, Compose BOM 2026.02.01); navigation-compose removed (not used, spec assumption 3) |
| `app/build.gradle.kts` | added | `namespace`/`applicationId` = `com.klasly.app`, `minSdk = 26`, compile SDK 37.1, target 36 |
| `app/src/main/AndroidManifest.xml` | added | Wizard manifest with `Theme.Klasly` (permissions come in Task 1) |
| `app/src/main/java/com/klasly/app/MainActivity.kt` | added | Fresh Empty Activity "Hello Android" screen |
| `app/src/main/java/com/klasly/app/ui/theme/Color.kt`, `Theme.kt`, `Type.kt` | added | Wizard Material 3 theme, renamed to `KlaslyTheme` |
| `app/src/main/res/**`, `app/src/main/keepRules/rules.keep` | added | Launcher icons, strings, theme, backup rules, R8 rules file |
| `.gitignore`, `app/.gitignore` | added | Keep `local.properties`, `.gradle/` and `build/` out of git |
| `.gitattributes` | added | `gradlew` stays LF so it runs on macOS/Linux; `.bat` stays CRLF |
| `local.properties` | added, **not committed** | `sdk.dir=D:\Android\sdk` for this PC only |
| `SPEC.md`, `tasks/plan.md`, `tasks/todo.md` (repo root) | modified | Records the change to open question 1 and ticks Task 0 |

## Key code

```kotlin
// app/build.gradle.kts
android {
    namespace = "com.klasly.app"
    compileSdk { version = release(37) { minorApiLevel = 1 } }
    defaultConfig {
        applicationId = "com.klasly.app"
        minSdk = 26          // CLAUDE.md §3.6
        targetSdk = 36
    }
}
```

AGP 9 has Kotlin support built in, so there's no separate `kotlin-android` plugin, only the Compose compiler plugin.

## Database changes
None.

## How it was verified
- `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew assembleDebug` → **BUILD SUCCESSFUL** (first run 7 m 40 s, mostly downloads).
- `git check-ignore` confirms `local.properties`, `app/build` and `.gradle` are ignored; `gradlew` is committed with LF.
- Not yet installed on an emulator. That happens at Checkpoint A, after Task 3.

## Known limits / TODO
- No unit test yet (Task 2 adds `ApiTextTest`). The wizard's example tests were not copied.
- App name is still "KlaslyApp" (Task 4 sets it to "Klasly").
- Commits: `b1f7b7e` (spec + plan), `aca56d0` (this shell).
