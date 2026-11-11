# Developer docs — Milestone 13 Task 8: Android parent Child overview

## Summary
The parent's placeholder screen ("Your child overview comes in a later update") is replaced by the **Child overview**:
- **Loading the children:** the screen loads the parent's linked children. With none, it says so. With one, that child's overview shows straight away. With several, a row of FilterChips switches between them.
- **Each class card:** the At risk / On track badge with the early-warning line and Manila "Updated" time, the attendance counts, the absence rate and the weighted average. Show / Hide buttons reveal the records and grades.
- **States:** loading, error with Try again, and pull-to-refresh, like the student tabs. Log out is in the header.

## Projects touched
ANDROID KOTLIN.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `.../ui/parent/ParentViewModel.kt` | added | `children`, `selectedId`, `state`, `subtitle`; `refresh()` reloads the children and the picked child; `select(id)`. |
| `.../ui/parent/ParentHomeScreen.kt` | modified | Rewritten: `OverviewContent` + header with child chips + `ChildClassCard`. |
| `.../MainActivity.kt` | modified | Parent → `ParentHomeScreen(viewModel(key = token) { ParentViewModel(container) })`. |

The card itself (`ChildClassCard`) was added with the shared cards in Task 7 (`ui/overview/ClassCards.kt`).

## Key code
```kotlin
val keep = result.value.firstOrNull { it.studentId == _selectedId.value } ?: result.value.firstOrNull()
```
A refresh keeps the child the parent was looking at. If that child was unlinked meanwhile, it falls back to the first child.

```kotlin
fun select(studentId: Int) {
    ...
    _state.value = OverviewUiState.Loading // don't show the previous child's cards under the new name
    loading?.cancel()
    loading = viewModelScope.launch { loadOverview(studentId) }
}
```
Cancelling the running load means a slow answer for the previous child can never overwrite the newly picked one.

## Database changes
None.

## How it was verified
- `./gradlew assembleDebug testDebugUnitTest` → BUILD SUCCESSFUL; unit tests 11/11.
- The endpoints the screen calls were checked live in Task 2 (parent Marites: children [1, 2], child 1's overview, 403 for child 3). The on-device check is Task 9.

## Known limits / TODO
- No unit test for `ParentViewModel`: it needs a fake `AppContainer`, which the project doesn't have yet. The only branch is the one-line "keep or first" choice above.
- An API 403 (if a child is unlinked while the screen is open) shows as an error with Try again. Try again reloads the list, which drops that child.
