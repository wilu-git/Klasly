# How to use — Milestone 13 Task 5 (Parent portal polish)

The parent pages work the same as in Task 4, with small usability fixes.

## Prerequisites
Same as Task 4: SQL Server LocalDB, the Web API and the website running.

## Step by step
1. Log in at `https://localhost:7144` as `marites.mendoza@klasly.test` / `Klasly@123` (fictional).
2. Open a child. On a phone-sized window, tap **Attendance records** or **Grades**. The whole row is now an easy tap target.
3. In **Attendance records**, absences are shown in bold red so they stand out.
4. In **Grades**, assessments without a score say "Not graded yet" in grey.
5. With more than one child, the name buttons at the top are now the same height.

## Example
Attendance records for ELECT3, newest first:
| Session | Status |
| --- | --- |
| Nov 10, 9:00 AM | **Absent** (bold red) |
| Nov 9, 9:00 AM | Present |

## Troubleshooting
- **The page looks unstyled**: the browser may be caching the old CSS. Hard-refresh with Ctrl+F5.
- **"Klasly can't reach the server right now"**: start the Web API first, wait until it says "Now listening", then reload.
