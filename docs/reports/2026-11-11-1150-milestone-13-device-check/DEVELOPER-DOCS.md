# Developer docs — Milestone 13 device check (Pixel_7 emulator, second try)

## Summary
The on-device check that Task 9 left open is done. The Pixel_7 emulator was cold-booted **with its window**, which ran stably, unlike the earlier headless try. Both new Android screens worked end to end against the real Web API:
- the parent Child overview: cards, expanding records, switching child, pull-to-refresh;
- the student Scan / Attendance / Grades tabs.

The visual pass found one defect, now fixed: the Show/Hide buttons sat indented from the card text. Two documentation errors were also corrected. With this, every Milestone 13 success criterion in `SPEC.md` is ticked.

## Projects touched
ANDROID KOTLIN, docs.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `android/KlaslyApp/app/src/main/java/com/klasly/app/ui/overview/ClassCards.kt` | modified | `Expandable`: `contentPadding = PaddingValues(vertical = 12.dp)`, so "Show attendance records" lines up with the card's text; still ≥ 48 dp tall. |
| `docs/reports/…task-7…/HOW-TO-USE.md`, `…task-8…/HOW-TO-USE.md`, `…task-9…/HOW-TO-USE.md` | modified | The emulator needs `--launch-profile http`, not `https` (see below). Task 9's intro updated. |
| `SPEC.md` | modified | The two Android success criteria ticked. |
| `tasks/todo-m13.md`, `tasks/plan-m13.md` | modified | Task 9 smoke and Checkpoint B fully ticked. |
| `docs/reports/2026-11-11-1150-milestone-13-device-check/*.png` | added | Screenshots: parent overview, student attendance, student grades (fictional seed data). |

## Key code
```kotlin
// No side padding, so the label lines up with the card's text; the height keeps a 48 dp tap target.
TextButton(onClick = { open = !open }, contentPadding = PaddingValues(vertical = 12.dp), modifier = Modifier.heightIn(min = 48.dp)) {
```

## Why the first try failed: wrong API profile, not app code
- The headless emulator hit a System UI ANR and dropped keystrokes. With its window, it booted cleanly.
- More importantly, "Can't reach Klasly" came from the API, not the network:

  ```
  $ curl -i -X POST http://localhost:5201/api/auth/login …
  HTTP/1.1 307 Temporary Redirect
  Location: https://localhost:7256/api/auth/login
  ```

  The API running on the PC was started with the **https** profile, so `UseHttpsRedirection()` sends the app to `https://localhost:7256`, which the emulator can't reach. Milestone 8 had already documented this (start the API with `--launch-profile http`). Three Milestone 13 HOW-TO files said `https`, and they are now fixed.
- That API process (PID 13432) isn't from this session, so it was left running. For the check, a second **HTTP-only** API ran on port 5202 (`--no-launch-profile --urls http://localhost:5202`). A temporary `klasly.apiBaseUrl=http://10.0.2.2:5202/api/` line pointed the debug build at it. Afterwards, `local.properties` was restored from a backup, and the default build (`http://10.0.2.2:5201/api/`) was reinstalled.

## Database changes
None to the schema. Test data touched and restored:
- logging in as Carla registered the emulator as her phone;
- afterwards it was reset through the admin endpoint (`DELETE api/accounts/6/device` → 204), so Carla's `RegisteredDeviceId` is back to NULL;
- Aira's and Bryan's are unchanged.

## How it was verified
On the emulator (UI read with `uiautomator dump`, plus screenshots):

| Check | Result |
| --- | --- |
| Parent Marites logs in | Child overview for Aira: chips [Aira, Bryan], 3 cards, "At risk" badges with the early-warning line and "Updated Nov 11, 12:52 AM", counts and rate matching the web page (CPE 401 0·0·2·0 100%; ELECT3 4·1·12·1 75%, avg 90) |
| Show attendance records (2) | Expands to "Nov 10, 5:26 PM — Absent", "Nov 9, 9:40 PM — Absent" (Manila time); button becomes "Hide…" |
| Tap the Bryan chip | Overview switches to Bryan (2026-00002; ELECT3 0·1·16·0) |
| Pull to refresh | Reloads and stays on Bryan |
| Student Carla logs in | Scan tab: camera preview, status card above the bottom bar, header below the status bar (no doubled inset) |
| Attendance tab | "My attendance · Carla Dimaano · 1st Sem 2026-27", ELECT3 1·1·15·0, rate 88.2%, records newest first, Absent in bold red, **no at-risk badge** (spec Q2) |
| Grades tab | Midterm (Exam · 70%) "Not graded yet", Quiz 1 (Quiz · 30%) "20 / 20", weighted average 100 |
| Show/Hide alignment after the fix | Label left edge matches the card text |

`./gradlew installDebug testDebugUnitTest` → BUILD SUCCESSFUL, unit tests 11/11.

## Known limits / TODO
- Not checked: TalkBack (the screen reader) and a real phone. Repeat on the demo phone with the API on the `http` profile.
- Assessments with no `DatePosted` sort first in the grade list (SQL puts NULL first), which is why Midterm appears above Quiz 1. Left as is; change the ordering if the team prefers posted-first.
- After logout, the login screen keeps the last email typed. That's existing behaviour from Milestone 8, not changed here.
- The emulator window is still open; close it when done.
