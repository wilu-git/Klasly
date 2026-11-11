# How to use — Milestone 13 Task 6 (Android data layer)

There's nothing new to tap in the app yet. This task lets the app download a student's attendance and grades, and prepares the text the screens will show.

## Prerequisites
- Android Studio installed (its built-in Java is used to build).
- A terminal in `android/KlaslyApp`.

## Step by step
1. In PowerShell: `$env:JAVA_HOME = "D:\Android Studio\jbr"` (use your Android Studio folder).
2. Run the tests: `./gradlew testDebugUnitTest`. It should end with **BUILD SUCCESSFUL**.
3. Build the app: `./gradlew assembleDebug`.

## Example: how values will appear on screen
| Value from the API | Shown as |
| --- | --- |
| absence rate `0.2` | 20% |
| absence rate not computed yet | — |
| weighted average `87.5` | 87.5 |
| no grades yet | No grades yet |
| score `18` of `20` | 18 / 20 |
| not graded | Not graded yet |
| `2026-11-10T01:00:00Z` | Nov 10, 9:00 AM (Manila time) |

## Troubleshooting
- **"JAVA_HOME is not set"**: do step 1 first, in the same terminal.
- **First build is very slow**: Gradle downloads its tools once. Later builds take under a minute.
- **A test about time fails**: the expected times are Manila time (UTC+8, no daylight saving). Don't change the phone's or PC's time zone for the tests; the code always uses Asia/Manila.
