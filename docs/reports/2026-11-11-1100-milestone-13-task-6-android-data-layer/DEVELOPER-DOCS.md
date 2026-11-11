# Developer docs — Milestone 13 Task 6: Android data layer + display text

## Summary
The Android app can now call the three overview endpoints, and has small, unit-tested text helpers for the coming screens: percent, weighted average, score / max, and Manila date-time. It was written test first: `DisplayTextTest` (6 tests) failed with "Unresolved reference" before the code existed. No screens change yet (Tasks 7–8).

## Projects touched
ANDROID KOTLIN.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `android/KlaslyApp/app/src/main/java/com/klasly/app/data/Dtos.kt` | modified | `StudentOverview`, `ClassOverview`, `AttendanceItem`, `GradeItem`, `LinkedStudent` (nullable fields default to null). |
| `.../data/KlaslyApi.kt` | modified | `GET students/me/overview`, `GET parent/children`, `GET parent/children/{studentId}/overview`. |
| `.../data/KlaslyRepository.kt` | modified | `myOverview()`, `children()`, `childOverview(id)`, wrapped in the existing `call {}` (`ApiResult`, 401 → logout). |
| `.../data/DisplayText.kt` | added | `percent`, `average`, `score`, `manilaDateTime`. |
| `android/KlaslyApp/app/src/test/java/com/klasly/app/data/DisplayTextTest.kt` | added | Formatting cases + parsing a real overview body with nulls, and the children list. |

## Key code
```kotlin
fun percent(rate: Double?): String = rate?.let { oneDecimal.format(it * 100) + "%" } ?: "—"
fun average(value: Double?): String = value?.let(twoDecimals::format) ?: "No grades yet"
fun score(value: Double?, max: Double): String =
    value?.let { "${twoDecimals.format(it)} / ${twoDecimals.format(max)}" } ?: "Not graded yet"
fun manilaDateTime(utc: String): String = manila.format(parseUtc(utc))
```
- `DecimalFormat` uses US symbols, so a phone set to another locale still shows "87.5", not "87,5".
- `manilaDateTime` reuses `parseUtc` from `ApiText.kt`, which also reads SQL Server times that have no "Z".

## Database changes
None.

## How it was verified
- `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew testDebugUnitTest`:
  - before the code: compile errors ("Unresolved reference 'percent'"), so the tests were red;
  - after: BUILD SUCCESSFUL, with `ApiTextTest` 5/5 and `DisplayTextTest` 6/6.
- `./gradlew assembleDebug` → BUILD SUCCESSFUL.

## Known limits / TODO
- The screens that use this come in Task 7 (student tabs) and Task 8 (parent).
- This shell has no `java` on PATH. Set `JAVA_HOME` to Android Studio's `jbr` before running Gradle (the same note as Milestone 8).
