# Developer docs — Milestone 13 Task 7: Android student tabs

## Summary
The student app now has a bottom navigation bar with **Scan** (unchanged), **Attendance** and **Grades**:
- **Attendance:** per class, the Present · Late · Absent · Excused counts, the absence rate and every record (Manila time; absences in bold red).
- **Grades:** per class, each assessment with score / max, type and weight, plus the weighted average.
- **Shared behaviour:** both tabs read one `StudentOverviewViewModel`, so switching tabs doesn't reload. Each tab shows loading, error (with Try again), "No classes this term" and pull-to-refresh states.
- **No risk badge for students:** the student's overview has `isAtRisk` null (spec Q2), so none is drawn.

## Projects touched
ANDROID KOTLIN.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `.../ui/overview/ClassCards.kt` | added | `OverviewUiState`, `OverviewContent` (states + `PullToRefreshBox`), `OverviewHeader`, `AttendanceCard`, `GradesCard`, `ChildClassCard` (Task 8 uses it). Shared by both roles. |
| `.../ui/student/StudentOverviewViewModel.kt` | added | Loads `students/me/overview`; keeps the data during a pull-to-refresh. |
| `.../ui/student/StudentHome.kt` | added | `Scaffold` + Material 3 `NavigationBar`. The tab is kept in `rememberSaveable`, so it survives rotation. Also holds the Attendance and Grades tab bodies (the plan's separate `AttendanceTab.kt` / `GradesTab.kt` were folded in; each is three lines). |
| `.../MainActivity.kt` | modified | Student → `StudentHome(overview) { ScanScreen(...) }`. View models are keyed by token, so the next login starts fresh. |
| `app/src/main/res/drawable/ic_tab_scan.xml`, `ic_tab_attendance.xml`, `ic_tab_grades.xml` | added | Tab icons: Material Icons vector paths (Apache 2.0). The project has no icon library and none was added (spec Q6). |

## Key code
```kotlin
// The Scan screen draws under the status bar itself; only keep it above the bottom bar.
Tab.Scan -> {
    val bottom = PaddingValues(bottom = inner.calculateBottomPadding())
    Box(Modifier.padding(bottom).consumeWindowInsets(bottom)) { scan() }
}
```
Consuming only the bottom inset stops the Scan screen's own `navigationBarsPadding()` from adding a second gap above the bar, while its header still sits below the status bar.

```kotlin
fun refresh() {
    val current = _state.value
    _state.value = if (current is OverviewUiState.Ready) current.copy(refreshing = true) else OverviewUiState.Loading
    ...
}
```

## Database changes
None.

## How it was verified
- `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew assembleDebug testDebugUnitTest` → BUILD SUCCESSFUL, no warnings in the new files. Unit tests: 11/11 (ApiTextTest 5, DisplayTextTest 6).
- No new unit test: the new code is Compose layout and a view model that only maps `ApiResult` to a state; the formatting it uses is tested in Task 6. The on-device check is Task 9.

## Known limits / TODO
- A failed refresh replaces the list with the error and Try again. It does not keep the old data on screen.
- The emulator check and the `/impeccable` pass are Task 9.
