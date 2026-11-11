# Developer docs — Milestone 13 Task 9 + Checkpoint B: Android polish and milestone check

## Summary
- **Task 9:** a static `/impeccable` review of the Compose screens (student tabs, parent Child overview) found nothing to change. The on-device check could **not** be completed from this session: the headless Pixel_7 emulator was too unstable to drive (details below). The emulator check and a visual pass on a device remain open for the team.
- **Checkpoint B:** every spec success criterion is met except the two Android ones, which wait on that device check. No migration, no new package, all builds and tests pass.

## Projects touched
ANDROID KOTLIN (review only), docs.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `SPEC.md` | modified | Success criteria ticked; the two Android criteria left open with a note. |
| `tasks/plan-m13.md`, `tasks/todo-m13.md` | modified | Task 9 and Checkpoint B status; the open device check is marked "Open (team)". |

No source code changed in this task.

## Key code
None. The static review checked, in `ui/overview/ClassCards.kt`, `ui/student/StudentHome.kt` and `ui/parent/ParentHomeScreen.kt`:
- **Touch targets:** buttons, chips and Show/Hide toggles all use `heightIn(min = 48.dp)`; the bottom bar is Material 3's own.
- **Screen reader:** screen and class titles are marked `heading()`; tab icons have no description because the label names them; the badge is real text ("At risk" / "On track"), not just colour.
- **Contrast:** white on the fixed `0xFF1B6E34` green (same as the Scan screen) and `onError` on `error`.
- **States:** loading, error with Try again, empty, "No classes this term" and pull-to-refresh on every screen.

## Database changes
None. No file under `Klasly.Core/Entities`, `Klasly.Infrastructure/Data` or `Migrations` changed in Milestone 13 (`git diff b0c8beb` → 0 files). Task 1's `has-pending-model-changes` run reported no changes.

## How it was verified
- `dotnet build Klasly.slnx --artifacts-path <scratch>` → Build succeeded. A separate output folder was needed because a running `Klasly.API` process (PID 13432, started 10:24, not started by this session) locks `src/Klasly.API/bin`, and a normal in-place build fails to copy DLLs.
- `dotnet test --solution Klasly.slnx --artifacts-path <scratch>` → 120 passed, 0 failed.
- `./gradlew assembleDebug testDebugUnitTest` (JAVA_HOME = Android Studio jbr) → BUILD SUCCESSFUL, 11/11 unit tests.
- `dotnet list Klasly.slnx package`: Microsoft.* all 10.0.14. No `.csproj` or Gradle dependency file changed since the milestone's plan commit (`b0c8beb`).
- **Emulator attempt (failed, not a pass):**
  - the Pixel_7 AVD started headless and the debug APK installed (`installDebug` → "Installed on 1 device");
  - System UI raised an ANR on boot; `uiautomator dump` timed out; `input text` dropped characters ("marite.m" for the email);
  - one login reached the app's "Can't reach Klasly. Check your connection." The API answered 401 on `localhost:5201` from the host, so the emulator's network was probably not up yet;
  - later launches didn't take. The crash buffer held only the `uiautomator` timeout, with no app crash. The emulator was shut down.

## Known limits / TODO
- **Open (team):**
  - with Android Studio's emulator *window* (not headless) or a real phone, log in as student `aira.mendoza@klasly.test` (her registered phone, or Carla if that phone isn't at hand). Check that the Attendance and Grades tabs match the web data and that pull to refresh works;
  - log in as parent `marites.mendoza@klasly.test` and check the chips switch between Aira and Bryan;
  - check TalkBack reads the badges.
- The web one-child redirect is also unexercised live (Task 5 report).
- A stray API process (PID 13432) is still running and locking the build output. Stop it before the next normal `dotnet build`.
